using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
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

    private readonly string _database = $"project_test_{Guid.NewGuid():N}"[..28];

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Database:Provider", server.Provider);
        builder.UseSetting("Database:ConnectionStrings:Default", server.ConnectionString(_database));
        builder.UseSetting("Database:MigrateOnStartup", "true");
        builder.UseSetting("Database:MaxRetryCount", "0");
        builder.UseSetting("Jwt:SigningKey", "functional-tests-signing-key-0123456789abcdef");
        builder.UseSetting("Seed:AdminPassword", AdminPassword);
        builder.UseSetting("Seed:SampleData", "false");
        builder.UseSetting("RateLimiting:Enabled", "false");
        builder.UseSetting("Outbox:PollingIntervalSeconds", "1");
        builder.UseSetting("Tenancy:Mode", tenancyMode);
        builder.UseSetting($"Secrets:{DedicatedSecret}", server.ConnectionString(_database + "_d"));
        builder.UseSetting("Features:platform.admin", "true");

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
