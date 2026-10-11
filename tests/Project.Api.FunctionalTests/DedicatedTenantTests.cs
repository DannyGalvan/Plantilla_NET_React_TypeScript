using System.Net;
using System.Net.Http.Json;
using Project.Api.FunctionalTests.Infrastructure;

namespace Project.Api.FunctionalTests;

/// <summary>
/// <c>Tenancy:Mode=Hybrid</c>: a dedicated tenant gets its own database, created and migrated on provisioning,
/// while shared tenants stay in the platform database.
/// </summary>
public sealed class DedicatedTenantTests(HybridModeApi api) : IClassFixture<HybridModeApi>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Dedicated_tenants_are_provisioned_and_isolated_in_their_own_database()
    {
        var platform = await api.SignInAsync();
        var tenant = await (await platform.PostAsJsonAsync("/api/v1/tenancy/tenants",
            new { code = "vip", name = "Vip Corp", isolation = "Dedicated", connectionSecretRef = ApiFactory.DedicatedSecret, primaryDomain = "vip.test" }, Ct)).ReadJsonAsync();
        tenant["status"]!.GetValue<string>().ShouldBe("Provisioning");
        tenant["hasDedicatedConnection"]!.GetValue<bool>().ShouldBeTrue();
        tenant.ContainsKey("connectionSecretRef").ShouldBeFalse();

        // A tenant in provisioning cannot be used yet.
        (await api.Client("vip.test").PostAsJsonAsync("/api/v1/identity/auth/login", new { userName = "admin", password = ApiFactory.AdminPassword }, Ct))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var provisionResponse = await platform.PostAsync($"/api/v1/tenancy/tenants/{tenant.Id()}/provision", null, Ct);
        provisionResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await provisionResponse.Content.ReadAsStringAsync(Ct));
        var provisioned = await provisionResponse.ReadJsonAsync();
        provisioned["status"]!.GetValue<string>().ShouldBe("Active");

        var vip = await api.SignInAsync(host: "vip.test");
        (await vip.PostAsJsonAsync("/api/v1/catalog/categories", new { name = "Dedicated data" }, Ct)).StatusCode.ShouldBe(HttpStatusCode.Created);
        var vipList = await (await vip.GetAsync("/api/v1/catalog/categories?filter=name:eq:Dedicated data", Ct)).ReadJsonAsync();
        vipList["total"]!.GetValue<long>().ShouldBe(1);

        var shared = await api.SignInAsync();
        var sharedList = await (await shared.GetAsync("/api/v1/catalog/categories?filter=name:eq:Dedicated data", Ct)).ReadJsonAsync();
        sharedList["total"]!.GetValue<long>().ShouldBe(0);

        var report = await (await vip.GetAsync("/api/v1/catalog/reports/stock", Ct)).ReadArrayAsync();
        report.ShouldContain(r => r!["categoryName"]!.GetValue<string>() == "Dedicated data");
    }
}
