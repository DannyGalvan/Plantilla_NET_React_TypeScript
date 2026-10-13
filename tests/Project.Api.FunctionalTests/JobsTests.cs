using System.Net;
using System.Net.Http.Json;
using Project.Api.FunctionalTests.Infrastructure;

namespace Project.Api.FunctionalTests;

/// <summary>Jobs module in a multi-tenant deployment: runs are scoped to the tenant that asked for them.</summary>
public sealed class JobsTests(SharedModeApi api) : IClassFixture<SharedModeApi>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Jobs_run_inside_their_tenant_and_keep_a_history()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var host = $"jobs{suffix}.test";
        var platform = await api.SignInAsync();
        var tenant = await (await platform.PostAsJsonAsync("/api/v1/tenancy/tenants",
            new { code = $"jobs{suffix}", name = "Jobs", isolation = "Shared", primaryDomain = host }, Ct)).ReadJsonAsync();
        (await platform.PostAsync($"/api/v1/tenancy/tenants/{tenant.Id()}/provision", null, Ct)).EnsureSuccessStatusCode();
        var admin = await api.SignInAsync(host: host);

        var jobs = await (await admin.GetAsync("/api/v1/jobs", Ct)).ReadArrayAsync();
        jobs.Select(j => j!["name"]!.GetValue<string>())
            .ShouldBe(["audit.retention", "files.purge-deleted", "notifications.cleanup", "tests.recording"], ignoreOrder: true);

        (await admin.PutAsJsonAsync("/api/v1/jobs/tests.recording", new { enabled = true, cron = "not a cron" }, Ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await admin.PostAsync("/api/v1/jobs/tests.recording/run", null, Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        for (var attempt = 0; attempt < 50 && !api.JobRuns.Tenants.Contains(tenant.Id()); attempt++)
        {
            await Task.Delay(200, Ct);
        }

        api.JobRuns.Tenants.ShouldContain(tenant.Id());
        api.JobRuns.Tenants.Count(t => t == tenant.Id()).ShouldBe(1);

        var runs = await WaitForRunsAsync(admin);
        runs[0]!["status"]!.GetValue<string>().ShouldBe("succeeded");

        (await admin.PutAsJsonAsync("/api/v1/jobs/tests.recording", new { enabled = false, cron = "*/5 * * * *" }, Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var paused = (await (await admin.GetAsync("/api/v1/jobs", Ct)).ReadArrayAsync()).Single(j => j!["name"]!.GetValue<string>() == "tests.recording")!;
        paused["enabled"]!.GetValue<bool>().ShouldBeFalse();
        paused["cron"]!.GetValue<string>().ShouldBe("*/5 * * * *");
    }

    private static async Task<System.Text.Json.Nodes.JsonArray> WaitForRunsAsync(HttpClient client)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var runs = await (await client.GetAsync("/api/v1/jobs/tests.recording/runs", Ct)).ReadArrayAsync();
            if (runs.Count > 0)
            {
                return runs;
            }

            await Task.Delay(200, Ct);
        }

        throw new TimeoutException("The job did not record a run.");
    }
}
