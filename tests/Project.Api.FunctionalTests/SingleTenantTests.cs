using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Project.Api.FunctionalTests.Infrastructure;

[assembly: AssemblyFixture(typeof(DatabaseServer))]

namespace Project.Api.FunctionalTests;

/// <summary>
/// End-to-end behavior in <c>Tenancy:Mode=None</c> (single company): security, generic CRUD, raw SQL and
/// organization scope, against a real database engine.
/// </summary>
public sealed class SingleTenantTests(NoneModeApi api) : IClassFixture<NoneModeApi>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Health_and_platform_config_are_public()
    {
        var client = api.Client();
        (await client.GetAsync("/health/live", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetAsync("/health/ready", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var config = await (await client.GetAsync("/api/v1/platform/config", Ct)).ReadJsonAsync();
        config["tenancyMode"]!.GetValue<string>().ShouldBe("None");
        config["organizationEnabled"]!.GetValue<bool>().ShouldBeTrue();
    }

    [Fact]
    public async Task Api_reference_is_not_exposed_outside_development()
    {
        (await api.Client().GetAsync("/openapi/v1.json", Ct)).StatusCode.ShouldNotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Protected_endpoints_require_a_token()
    {
        var response = await api.Client().GetAsync("/api/v1/catalog/products", Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Invalid_credentials_return_a_generic_401()
    {
        var response = await api.Client().PostAsJsonAsync("/api/v1/identity/auth/login", new { userName = "nobody", password = "x" }, Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, await response.Content.ReadAsStringAsync(Ct));
        (await response.CodeAsync()).ShouldBe("auth.invalid_credentials");
    }

    [Fact]
    public async Task Admin_profile_contains_permissions_and_forces_password_change()
    {
        var admin = await api.SignInAsync();
        var me = await (await admin.GetAsync("/api/v1/me", Ct)).ReadJsonAsync();

        me["mustChangePassword"]!.GetValue<bool>().ShouldBeTrue();
        me["permissions"]!.AsArray().Select(p => p!.GetValue<string>()).ShouldContain("catalog.products.create");
        me["isPlatformAdmin"]!.GetValue<bool>().ShouldBeTrue();
    }

    [Fact]
    public async Task Generic_crud_supports_filter_sort_paging_patch_concurrency_and_soft_delete()
    {
        var admin = await api.SignInAsync();
        var category = await (await admin.PostAsJsonAsync("/api/v1/catalog/categories", new { name = "Crud tests" }, Ct)).ReadJsonAsync();

        foreach (var (sku, price) in new[] { ("CRUD-1", 10m), ("CRUD-2", 20m), ("CRUD-3", 30m) })
        {
            var created = await admin.PostAsJsonAsync("/api/v1/catalog/products", new { sku, name = $"Product {sku}", price, stock = 5, categoryId = category.Id() }, Ct);
            created.StatusCode.ShouldBe(HttpStatusCode.Created);
        }

        var page = await (await admin.GetAsync("/api/v1/catalog/products?filter=sku:startswith:CRUD;price:gte:20&sort=-price&pageSize=1000", Ct)).ReadJsonAsync();
        page["pageSize"]!.GetValue<int>().ShouldBe(100);
        page["total"]!.GetValue<long>().ShouldBe(2);
        page["items"]!.AsArray().Select(i => i!["sku"]!.GetValue<string>()).ShouldBe(["CRUD-3", "CRUD-2"]);

        var product = page["items"]![0]!.AsObject();
        var id = product.Id();
        var stamp = product["concurrencyStamp"]!.GetValue<string>();

        var patched = await (await admin.PatchAsJsonAsync($"/api/v1/catalog/products/{id}", new { stock = 0 }, Ct)).ReadJsonAsync();
        patched["stock"]!.GetValue<int>().ShouldBe(0);

        var stale = await admin.PutAsJsonAsync($"/api/v1/catalog/products/{id}",
            new { name = "Stale", price = 1, stock = 1, categoryId = category.Id(), isActive = true, concurrencyStamp = stamp }, Ct);
        stale.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        (await admin.DeleteAsync($"/api/v1/catalog/products/{id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await admin.GetAsync($"/api/v1/catalog/products/{id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await admin.PostAsync($"/api/v1/catalog/products/{id}/restore", null, Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await admin.GetAsync($"/api/v1/catalog/products/{id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Invalid_input_returns_problem_details_with_field_errors()
    {
        var admin = await api.SignInAsync();

        var invalid = await admin.PostAsJsonAsync("/api/v1/catalog/products", new { sku = "", name = "", price = -1, stock = 0, categoryId = Guid.Empty }, Ct);
        invalid.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await invalid.ReadJsonAsync();
        problem["errors"]!.AsObject().Select(e => e.Key).ShouldContain("price");
        problem["traceId"].ShouldNotBeNull();

        var sensitive = await admin.GetAsync("/api/v1/catalog/products?filter=tenantId:eq:00000000-0000-0000-0000-000000000001", Ct);
        sensitive.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        (await admin.GetAsync($"/api/v1/catalog/products/{Guid.NewGuid()}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Dapper_report_and_stored_procedure_run_on_the_engine()
    {
        var admin = await api.SignInAsync();
        var category = await (await admin.PostAsJsonAsync("/api/v1/catalog/categories", new { name = "Report tests" }, Ct)).ReadJsonAsync();
        await admin.PostAsJsonAsync("/api/v1/catalog/products", new { sku = "LOW-1", name = "Low stock", price = 2.5m, stock = 1, categoryId = category.Id() }, Ct);

        var report = await (await admin.GetAsync("/api/v1/catalog/reports/stock", Ct)).ReadArrayAsync();
        var row = report.Single(r => r!["categoryName"]!.GetValue<string>() == "Report tests")!;
        row["products"]!.GetValue<long>().ShouldBe(1);
        row["inventoryValue"]!.GetValue<decimal>().ShouldBe(2.5m);

        var lowStock = await (await admin.GetAsync("/api/v1/catalog/reports/low-stock?threshold=2", Ct)).ReadArrayAsync();
        lowStock.Select(r => r!["sku"]!.GetValue<string>()).ShouldContain("LOW-1");
    }

    [Fact]
    public async Task Permission_changes_apply_without_signing_in_again()
    {
        var admin = await api.SignInAsync();
        var roles = await (await admin.GetAsync("/api/v1/identity/roles?filter=name:eq:User", Ct)).ReadJsonAsync();
        var userRole = roles["items"]![0]!.AsObject();

        var created = await admin.PostAsJsonAsync("/api/v1/identity/users",
            new { userName = "perm.user", email = "perm.user@example.com", password = "Secure.Pass-2026", roleIds = new[] { userRole.Id() }, mustChangePassword = false }, Ct);
        created.StatusCode.ShouldBe(HttpStatusCode.Created);

        var user = await api.SignInAsync("perm.user", "Secure.Pass-2026");
        (await user.PostAsJsonAsync("/api/v1/catalog/categories", new { name = "Denied" }, Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var permissions = userRole["permissions"]!.AsArray().Select(p => p!.GetValue<string>()).Append("catalog.categories.create").ToArray();
        (await admin.PutAsJsonAsync($"/api/v1/identity/roles/{userRole.Id()}/permissions", new { permissions }, Ct)).EnsureSuccessStatusCode();

        (await user.PostAsJsonAsync("/api/v1/catalog/categories", new { name = "Allowed" }, Ct)).StatusCode.ShouldBe(HttpStatusCode.Created);
        (await user.GetAsync("/api/v1/identity/users", Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Account_is_locked_after_repeated_failures()
    {
        var admin = await api.SignInAsync();
        (await admin.PostAsJsonAsync("/api/v1/identity/users",
            new { userName = "lock.user", email = "lock.user@example.com", password = "Secure.Pass-2026", mustChangePassword = false }, Ct)).EnsureSuccessStatusCode();

        var client = api.Client();
        for (var i = 0; i < 5; i++)
        {
            await client.PostAsJsonAsync("/api/v1/identity/auth/login", new { userName = "lock.user", password = "Wrong.Pass-2026" }, Ct);
        }

        var locked = await client.PostAsJsonAsync("/api/v1/identity/auth/login", new { userName = "lock.user", password = "Secure.Pass-2026" }, Ct);
        (await locked.CodeAsync()).ShouldBe("auth.locked_out");
    }

    [Fact]
    public async Task Refresh_tokens_rotate_require_csrf_and_detect_reuse()
    {
        var client = api.Client();
        var login = await client.PostAsJsonAsync("/api/v1/identity/auth/login", new { userName = "admin", password = ApiFactory.AdminPassword }, Ct);
        var cookies = Cookies(login);

        var withoutCsrf = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/auth/refresh");
        withoutCsrf.Headers.Add("Cookie", $"rt={cookies["rt"]}");
        (await client.SendAsync(withoutCsrf, Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var first = await RefreshAsync(client, cookies);
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        Cookies(first)["rt"].ShouldNotBe(cookies["rt"]);

        var reused = await RefreshAsync(client, cookies);
        reused.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await reused.CodeAsync()).ShouldBe("auth.refresh_token_reused");

        // The whole family was revoked: the rotated token is no longer valid either.
        (await RefreshAsync(client, Cookies(first))).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Password_change_enforces_policy_and_history()
    {
        var admin = await api.SignInAsync();
        (await admin.PostAsJsonAsync("/api/v1/identity/users",
            new { userName = "pwd.user", email = "pwd.user@example.com", password = "First.Pass-2026", mustChangePassword = true }, Ct)).EnsureSuccessStatusCode();

        var user = await api.SignInAsync("pwd.user", "First.Pass-2026");
        (await user.PostAsJsonAsync("/api/v1/identity/auth/change-password", new { currentPassword = "First.Pass-2026", newPassword = "weak" }, Ct))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await user.PostAsJsonAsync("/api/v1/identity/auth/change-password", new { currentPassword = "First.Pass-2026", newPassword = "Second.Pass-2026" }, Ct))
            .StatusCode.ShouldBe(HttpStatusCode.OK);

        var again = await api.SignInAsync("pwd.user", "Second.Pass-2026");
        var reuse = await again.PostAsJsonAsync("/api/v1/identity/auth/change-password", new { currentPassword = "Second.Pass-2026", newPassword = "First.Pass-2026" }, Ct);
        reuse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Users_only_see_rows_of_their_organization_units()
    {
        var admin = await api.SignInAsync();
        var root = (await (await admin.GetAsync("/api/v1/organization/units", Ct)).ReadArrayAsync()).First(u => u!["parentId"] is null)!.AsObject();
        var brandA = await (await admin.PostAsJsonAsync("/api/v1/organization/units", new { typeCode = "brand", parentId = root.Id(), code = "iso-a", name = "Iso A" }, Ct)).ReadJsonAsync();
        var brandB = await (await admin.PostAsJsonAsync("/api/v1/organization/units", new { typeCode = "brand", parentId = root.Id(), code = "iso-b", name = "Iso B" }, Ct)).ReadJsonAsync();
        var category = await (await admin.PostAsJsonAsync("/api/v1/catalog/categories", new { name = "Iso" }, Ct)).ReadJsonAsync();

        await admin.PostAsJsonAsync("/api/v1/catalog/products", new { sku = "ISO-A", name = "A", price = 1, stock = 1, categoryId = category.Id(), organizationUnitId = brandA.Id() }, Ct);
        var productB = await (await admin.PostAsJsonAsync("/api/v1/catalog/products",
            new { sku = "ISO-B", name = "B", price = 1, stock = 1, categoryId = category.Id(), organizationUnitId = brandB.Id() }, Ct)).ReadJsonAsync();

        var roles = await (await admin.GetAsync("/api/v1/identity/roles?filter=name:eq:User", Ct)).ReadJsonAsync();
        var user = await (await admin.PostAsJsonAsync("/api/v1/identity/users",
            new { userName = "brand.user", email = "brand.user@example.com", password = "Secure.Pass-2026", roleIds = new[] { roles["items"]![0]!.AsObject().Id() }, mustChangePassword = false }, Ct)).ReadJsonAsync();
        (await admin.PostAsJsonAsync($"/api/v1/organization/units/{brandA.Id()}/users", new { userId = user.Id() }, Ct)).EnsureSuccessStatusCode();

        var brandUser = await api.SignInAsync("brand.user", "Secure.Pass-2026");
        var visible = await (await brandUser.GetAsync("/api/v1/catalog/products?filter=sku:startswith:ISO", Ct)).ReadJsonAsync();
        visible["items"]!.AsArray().Select(p => p!["sku"]!.GetValue<string>()).ShouldBe(["ISO-A"]);
        (await brandUser.GetAsync($"/api/v1/catalog/products/{productB.Id()}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // Raw SQL (Dapper and the stored procedure) is filtered by unit too.
        var report = await brandUser.GetAsync("/api/v1/catalog/reports/stock", Ct);
        report.StatusCode.ShouldBe(HttpStatusCode.OK, await report.Content.ReadAsStringAsync(Ct));
        var isoRow = (await report.ReadArrayAsync()).Single(r => r!["categoryName"]!.GetValue<string>() == "Iso")!;
        isoRow["products"]!.GetValue<long>().ShouldBe(1);
        var lowStock = await (await brandUser.GetAsync("/api/v1/catalog/reports/low-stock?threshold=5", Ct)).ReadArrayAsync();
        lowStock.Select(r => r!["sku"]!.GetValue<string>()).Where(sku => sku.StartsWith("ISO", StringComparison.Ordinal)).ShouldBe(["ISO-A"]);

        var switchToB = await brandUser.PostAsJsonAsync("/api/v1/identity/auth/switch-context",
            new { tenantId = "00000000-0000-0000-0000-000000000001", organizationUnitId = brandB.Id() }, Ct);
        switchToB.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Tenant_settings_change_the_public_branding()
    {
        var admin = await api.SignInAsync();
        var updated = await admin.PutAsJsonAsync("/api/v1/tenancy/settings/branding?scope=Tenant", new { displayName = "Single Co", primaryColor = "#0f766e" }, Ct);
        updated.StatusCode.ShouldBe(HttpStatusCode.OK);

        var config = await (await api.Client().GetAsync("/api/v1/platform/config", Ct)).ReadJsonAsync();
        config["branding"]!["displayName"]!.GetValue<string>().ShouldBe("Single Co");

        (await admin.PutAsJsonAsync("/api/v1/tenancy/settings/branding", new { primaryColor = "url(javascript:x)" }, Ct))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private static Task<HttpResponseMessage> RefreshAsync(HttpClient client, IReadOnlyDictionary<string, string> cookies)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/auth/refresh");
        request.Headers.Add("Cookie", $"rt={cookies["rt"]}; XSRF-TOKEN={cookies["XSRF-TOKEN"]}");
        request.Headers.Add("X-XSRF-TOKEN", cookies["XSRF-TOKEN"]);
        return client.SendAsync(request, Ct);
    }

    private static Dictionary<string, string> Cookies(HttpResponseMessage response) =>
        response.Headers.GetValues("Set-Cookie")
            .Select(c => c.Split(';')[0].Split('=', 2))
            .ToDictionary(p => p[0], p => p[1]);
}
