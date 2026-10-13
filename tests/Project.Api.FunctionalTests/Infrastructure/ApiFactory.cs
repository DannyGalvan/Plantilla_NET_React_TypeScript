using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Project.Application.Abstractions;
using Project.Application.Jobs;
using Project.Application.Sequences;
using Project.Application.Tenancy;
using Project.Application.Workflows;
using Serilog.Core;
using Serilog.Events;
using Testcontainers.MsSql;
using Testcontainers.MySql;
using Testcontainers.PostgreSql;

namespace Project.Api.FunctionalTests.Infrastructure;

/// <summary>
/// Real database server in Docker, selected with <c>TEST_DB_PROVIDER</c> (PostgreSql by default, SqlServer, MySql).
/// One container per test run; every API instance gets its own database.
/// </summary>
public sealed class DatabaseServer : IAsyncLifetime
{
    public string Provider { get; } = Environment.GetEnvironmentVariable("TEST_DB_PROVIDER") is { Length: > 0 } provider ? provider : "PostgreSql";

    private IDatabaseContainer? _container;

    public async ValueTask InitializeAsync()
    {
        _container = Provider switch
        {
            "SqlServer" => new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build(),
            "MySql" => new MySqlBuilder("mysql:8.4").WithUsername("root").WithPassword("Test_Passw0rd").Build(),
            _ => new PostgreSqlBuilder("postgres:17-alpine").Build(),
        };
        await _container.StartAsync();
    }

    public string ConnectionString(string database)
    {
        var raw = _container!.GetConnectionString();
        return Provider switch
        {
            "SqlServer" => raw.Replace("Database=master", $"Database={database}", StringComparison.OrdinalIgnoreCase) + ";TrustServerCertificate=True",
            "MySql" => System.Text.RegularExpressions.Regex.Replace(raw, "Database=[^;]+", $"Database={database}"),
            _ => System.Text.RegularExpressions.Regex.Replace(raw, "Database=[^;]+", $"Database={database}"),
        };
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}

/// <summary>The real API (Program.cs) on an in-memory server, against its own database.</summary>
public class ApiFactory(DatabaseServer server, string tenancyMode, string[] resolvers) : WebApplicationFactory<Program>
{
    /// <summary>Secret reference of the dedicated database used by the Hybrid tests.</summary>
    public const string DedicatedSecret = "tenant-dedicated-db";

    public const string AdminPassword = "Admin.Test-2026!";

    /// <summary>Master key of the test runs (32 random bytes in base64, like <c>openssl rand -base64 32</c>).</summary>
    public const string MasterKey = "q1Yw3n7P0fHk2bJ8sZr5Xv9Lm4Tc6Ud1Ea0Wg3Qi8No=";

    private readonly string _database = $"project_test_{Guid.NewGuid():N}"[..28];

    /// <summary>Errors logged by the server, to explain a failed assertion (the console output is not captured).</summary>
    public ServerErrorLog ServerErrors { get; } = new();

    /// <summary>Emails sent by the server under test (delivered by the outbox).</summary>
    public CapturedEmails Emails { get; } = new();

    /// <summary>Tenants in which the test recurring job ran.</summary>
    public RecordingJob.Log JobRuns { get; } = new();

    /// <summary>Approval outcomes received by the (test) owning module.</summary>
    public ConcurrentQueue<ApprovalCompletedEvent> Approvals { get; } = new();

    /// <summary>Webhook calls made by the server (no network involved).</summary>
    public WebhookReceiver WebhookCalls { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Database:Provider", server.Provider);
        builder.UseSetting("Database:ConnectionStrings:Default", server.ConnectionString(_database));
        builder.UseSetting("Database:MigrateOnStartup", "true");
        builder.UseSetting("Database:MaxRetryCount", "0");
        // No JWT key: it is generated on the first start and stored encrypted with the master key (like production).
        builder.UseSetting("Configuration:MasterKey", MasterKey);
        builder.UseSetting("Configuration:ReloadSeconds", "1");
        builder.UseSetting("Seed:AdminPassword", AdminPassword);
        builder.UseSetting("Seed:SampleData", "false");
        builder.UseSetting("Modules:Enabled:0", "Catalog");
        builder.UseSetting("Files:Default:Settings:rootPath", Path.Combine(Path.GetTempPath(), "project-tests", _database));
        builder.UseSetting("RateLimiting:Enabled", "false");
        builder.UseSetting("Outbox:PollingIntervalSeconds", "1");
        builder.UseSetting("Jobs:PollingIntervalSeconds", "1");
        builder.UseSetting("Webhooks:PollingIntervalSeconds", "1");
        builder.UseSetting("Tenancy:Mode", tenancyMode);
        builder.UseSetting($"Secrets:{DedicatedSecret}", server.ConnectionString(_database + "_d"));
        builder.UseSetting("Features:platform.admin", "true");
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<ILogEventSink>(ServerErrors);
            services.AddSingleton<IEmailSender>(Emails);
            services.AddSingleton(JobRuns);
            services.AddRecurringJob<RecordingJob>(RecordingJob.Name, "0 0 1 1 *", description: "Test job (yearly).");
            services.AddSequence(new SequenceDefinition("tests.invoice", "T-{yyyy}-{0000}", SequenceReset.Yearly));
            services.AddSingleton(Approvals);
            services.AddHttpClient("webhooks").ConfigurePrimaryHttpMessageHandler(() => new WebhookReceiver.Forwarder(WebhookCalls));
            services.AddScoped<IDomainEventHandler<ApprovalCompletedEvent>, RecordingApprovalHandler>();
            services.AddWorkflow(new WorkflowTemplate("tests.purchase", "tests", "Purchase",
            [
                new ApprovalStep("Manager", ApproverKind.Permission, "catalog.products.update"),
                new ApprovalStep("Finance", ApproverKind.Role, "Finance", MinAmount: 1000),
            ]));
        });

        for (var i = 0; i < resolvers.Length; i++)
        {
            builder.UseSetting($"Tenancy:Resolvers:{i}", resolvers[i]);
        }
    }

    /// <summary>Client whose requests carry <paramref name="host"/> (tenant resolution by host).</summary>
    public HttpClient Client(string host = "localhost")
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri($"http://{host}"), HandleCookies = false });
        client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
        return client;
    }

    public async Task<HttpClient> SignInAsync(string userName = "admin", string password = AdminPassword, string host = "localhost")
    {
        var client = Client(host);
        var response = await client.PostAsJsonAsync("/api/v1/identity/auth/login", new { userName, password });
        response.EnsureSuccessStatusCode();
        var session = await response.Content.ReadFromJsonAsync<JsonObject>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!["accessToken"]!.GetValue<string>());
        return client;
    }
}

public sealed class NoneModeApi(DatabaseServer server) : ApiFactory(server, "None", ["Host", "Subdomain", "Claim"]);

public sealed class SharedModeApi(DatabaseServer server) : ApiFactory(server, "SharedDatabase", ["Host", "Claim"]);

public sealed class HybridModeApi(DatabaseServer server) : ApiFactory(server, "Hybrid", ["Host", "Claim"]);

/// <summary>Recurring job of the tests: records the tenant of the scope it runs in.</summary>
public sealed class RecordingJob(RecordingJob.Log log, ITenantContext tenant) : IRecurringJob
{
    public const string Name = "tests.recording";

    public Task ExecuteAsync(CancellationToken ct)
    {
        log.Tenants.Enqueue(tenant.TenantId ?? Guid.Empty);
        return Task.CompletedTask;
    }

    public sealed class Log
    {
        public ConcurrentQueue<Guid> Tenants { get; } = new();
    }
}

/// <summary>Stands in for the endpoints of the tenants: records every webhook call and answers with <see cref="Status"/>.</summary>
public sealed class WebhookReceiver
{
    public ConcurrentQueue<(string Url, IReadOnlyDictionary<string, string> Headers, string Body)> Calls { get; } = new();

    public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;

    public async Task<(string Url, IReadOnlyDictionary<string, string> Headers, string Body)> WaitForAsync(
        Func<(string Url, IReadOnlyDictionary<string, string> Headers, string Body), bool> match,
        CancellationToken ct)
    {
        for (var attempt = 0; attempt < 75; attempt++)
        {
            if (Calls.FirstOrDefault(match) is { Url: not null } found)
            {
                return found;
            }

            await Task.Delay(200, ct);
        }

        throw new TimeoutException("The expected webhook was not delivered.");
    }

    /// <summary>New handler per HttpClient handler rotation; the receiver itself is never disposed.</summary>
    public sealed class Forwarder(WebhookReceiver receiver) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var headers = request.Headers.ToDictionary(h => h.Key, h => string.Join(',', h.Value), StringComparer.OrdinalIgnoreCase);
            receiver.Calls.Enqueue((request.RequestUri!.ToString(), headers, await request.Content!.ReadAsStringAsync(cancellationToken)));
            return new HttpResponseMessage(receiver.Status);
        }
    }
}

/// <summary>The "owning module" of the test workflow: records the outcomes it is told about.</summary>
public sealed class RecordingApprovalHandler(ConcurrentQueue<ApprovalCompletedEvent> outcomes) : IDomainEventHandler<ApprovalCompletedEvent>
{
    public Task HandleAsync(ApprovalCompletedEvent domainEvent, CancellationToken ct)
    {
        outcomes.Enqueue(domainEvent);
        return Task.CompletedTask;
    }
}

/// <summary>Email sender that keeps the messages instead of sending them.</summary>
public sealed class CapturedEmails : IEmailSender
{
    private readonly ConcurrentQueue<(string To, string Subject, string Body)> _messages = new();

    public IReadOnlyList<(string To, string Subject, string Body)> Messages => [.. _messages];

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct)
    {
        _messages.Enqueue((to, subject, htmlBody));
        return Task.CompletedTask;
    }

    /// <summary>Waits until an email that matches arrives (the outbox polls every second in the tests).</summary>
    public async Task<(string To, string Subject, string Body)> WaitForAsync(Func<(string To, string Subject, string Body), bool> match, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (Messages.FirstOrDefault(match) is { To: not null } found)
            {
                return found;
            }

            await Task.Delay(200, ct);
        }

        throw new TimeoutException("The expected email was not sent.");
    }
}

/// <summary>Serilog sink that keeps the errors logged by the server under test.</summary>
public sealed class ServerErrorLog : ILogEventSink
{
    private readonly ConcurrentQueue<string> _errors = new();

    public void Emit(LogEvent logEvent)
    {
        if (logEvent.Level >= LogEventLevel.Error)
        {
            _errors.Enqueue($"{logEvent.RenderMessage()}{Environment.NewLine}{logEvent.Exception}");
        }
    }

    public override string ToString() => string.Join(Environment.NewLine, _errors);
}

public static class HttpExtensions
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static async Task<JsonObject> ReadJsonAsync(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonObject>(Json))!;

    public static async Task<JsonArray> ReadArrayAsync(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonArray>(Json))!;

    public static async Task<string> CodeAsync(this HttpResponseMessage response) =>
        (await response.ReadJsonAsync())["code"]?.GetValue<string>() ?? string.Empty;

    public static Guid Id(this JsonObject json) => Guid.Parse(json["id"]!.GetValue<string>());
}

[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseServer>
{
    public const string Name = "database";
}
