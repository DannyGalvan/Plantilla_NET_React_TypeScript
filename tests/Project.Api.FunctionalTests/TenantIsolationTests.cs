using System.Net;
using System.Net.Http.Json;
using Project.Api.FunctionalTests.Infrastructure;

namespace Project.Api.FunctionalTests;

/// <summary>
/// <c>Tenancy:Mode=SharedDatabase</c>: two tenants in one database, resolved by host. Nothing of one tenant may be
/// read, listed, counted, modified or cached for the other. These tests block the pipeline.
/// </summary>
public sealed class TenantIsolationTests(SharedModeApi api) : IClassFixture<SharedModeApi>, IAsyncLifetime
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private string _acme = string.Empty;
    private string _globex = string.Empty;

    public async ValueTask InitializeAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        _acme = $"acme{suffix}.test";
        _globex = $"globex{suffix}.test";

        var platform = await api.SignInAsync();
        foreach (var (code, host) in new[] { ($"acme{suffix}", _acme), ($"globex{suffix}", _globex) })
        {
            var tenant = await (await platform.PostAsJsonAsync("/api/v1/tenancy/tenants",
                new { code, name = code, isolation = "Shared", primaryDomain = host }, Ct)).ReadJsonAsync();
            (await platform.PostAsync($"/api/v1/tenancy/tenants/{tenant.Id()}/provision", null, Ct)).EnsureSuccessStatusCode();
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Rows_of_one_tenant_are_invisible_to_the_other()
    {
        var acme = await api.SignInAsync(host: _acme);
        var globex = await api.SignInAsync(host: _globex);

        var category = await (await acme.PostAsJsonAsync("/api/v1/catalog/categories", new { name = "Acme only" }, Ct)).ReadJsonAsync();

        (await globex.GetAsync($"/api/v1/catalog/categories/{category.Id()}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var list = await (await globex.GetAsync("/api/v1/catalog/categories?filter=name:eq:Acme only", Ct)).ReadJsonAsync();
        list["total"]!.GetValue<long>().ShouldBe(0);
        (await globex.PutAsJsonAsync($"/api/v1/catalog/categories/{category.Id()}", new { name = "Hijacked" }, Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await globex.DeleteAsync($"/api/v1/catalog/categories/{category.Id()}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var report = await (await globex.GetAsync("/api/v1/catalog/reports/stock", Ct)).ReadArrayAsync();
        report.ShouldNotContain(r => r!["categoryName"]!.GetValue<string>() == "Acme only");

        (await acme.GetAsync($"/api/v1/catalog/categories/{category.Id()}", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_token_of_one_tenant_is_rejected_on_the_host_of_another()
    {
        var acme = await api.SignInAsync(host: _acme);
        var stolen = api.Client(_globex);
        stolen.DefaultRequestHeaders.Authorization = acme.DefaultRequestHeaders.Authorization;

        var response = await stolen.GetAsync("/api/v1/catalog/categories", Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await response.CodeAsync()).ShouldBe("tenant.mismatch");
    }

    [Fact]
    public async Task Members_of_one_tenant_cannot_sign_in_to_another()
    {
        var acme = await api.SignInAsync(host: _acme);
        var userName = $"member{Guid.NewGuid():N}"[..14];
        (await acme.PostAsJsonAsync("/api/v1/identity/users",
            new { userName, email = $"{userName}@example.com", password = "Secure.Pass-2026", mustChangePassword = false }, Ct)).EnsureSuccessStatusCode();

        var denied = await api.Client(_globex).PostAsJsonAsync("/api/v1/identity/auth/login", new { userName, password = "Secure.Pass-2026" }, Ct);
        denied.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var users = await (await (await api.SignInAsync(host: _globex)).GetAsync($"/api/v1/identity/users?filter=userName:eq:{userName}", Ct)).ReadJsonAsync();
        users["total"]!.GetValue<long>().ShouldBe(0);
    }

    [Fact]
    public async Task Settings_and_branding_are_resolved_per_host()
    {
        var acme = await api.SignInAsync(host: _acme);
        (await acme.PutAsJsonAsync("/api/v1/tenancy/settings/branding", new { displayName = "Acme ERP" }, Ct)).EnsureSuccessStatusCode();

        var acmeConfig = await (await api.Client(_acme).GetAsync("/api/v1/platform/config", Ct)).ReadJsonAsync();
        var globexConfig = await (await api.Client(_globex).GetAsync("/api/v1/platform/config", Ct)).ReadJsonAsync();

        acmeConfig["branding"]!["displayName"]!.GetValue<string>().ShouldBe("Acme ERP");
        globexConfig["branding"]!["displayName"]!.GetValue<string>().ShouldNotBe("Acme ERP");
        globexConfig["tenant"]!["code"]!.GetValue<string>().ShouldStartWith("globex");
    }

    [Fact]
    public async Task Unknown_hosts_get_no_tenant()
    {
        var client = api.Client("unknown.example");
        var config = await (await client.GetAsync("/api/v1/platform/config", Ct)).ReadJsonAsync();
        config["tenant"].ShouldBeNull();

        var login = await client.PostAsJsonAsync("/api/v1/identity/auth/login", new { userName = "admin", password = ApiFactory.AdminPassword }, Ct);
        login.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Files_of_one_tenant_are_invisible_to_the_other()
    {
        var acme = await api.SignInAsync(host: _acme);
        var globex = await api.SignInAsync(host: _globex);
        var file = await (await FilesTests.UploadAsync(acme, "acme.txt", "acme only")).ReadJsonAsync();

        (await globex.GetAsync($"/api/v1/files/{file.Id()}/content", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await globex.DeleteAsync($"/api/v1/files/{file.Id()}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var listed = await (await globex.GetAsync("/api/v1/files?search=acme", Ct)).ReadJsonAsync();
        listed["items"]!.AsArray().ShouldBeEmpty();
        (await acme.GetAsync($"/api/v1/files/{file.Id()}/content", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
