using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Project.Api.FunctionalTests.Infrastructure;

namespace Project.Api.FunctionalTests;

/// <summary>Audit module: every module's changes are recorded without code in the module.</summary>
public sealed class AuditTests(NoneModeApi api) : IClassFixture<NoneModeApi>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Changes_of_any_module_are_recorded_with_their_values()
    {
        var admin = await api.SignInAsync();
        var category = await (await admin.PostAsJsonAsync("/api/v1/catalog/categories", new { name = "Audited", description = "first" }, Ct)).ReadJsonAsync();
        var id = category.Id();
        (await admin.PutAsJsonAsync($"/api/v1/catalog/categories/{id}", new { name = "Audited v2", description = "first" }, Ct)).EnsureSuccessStatusCode();
        (await admin.DeleteAsync($"/api/v1/catalog/categories/{id}", Ct)).EnsureSuccessStatusCode();
        (await admin.PostAsync($"/api/v1/catalog/categories/{id}/restore", null, Ct)).EnsureSuccessStatusCode();

        var entries = await WaitForEntriesAsync(admin, $"entityType=catalog.category&entityId={id}", 4);
        entries.Select(e => e!["operation"]!.GetValue<string>()).ShouldBe(["restored", "deleted", "updated", "created"]);

        var updated = entries.Single(e => e!["operation"]!.GetValue<string>() == "updated")!;
        var change = updated["changes"]!.AsArray().Single()!;
        change["property"]!.GetValue<string>().ShouldBe("Name");
        change["oldValue"]!.GetValue<string>().ShouldBe("Audited");
        change["newValue"]!.GetValue<string>().ShouldBe("Audited v2");
        updated["userName"]!.GetValue<string>().ShouldBe("admin");
    }

    [Fact]
    public async Task Sign_ins_and_secrets_never_reach_the_history()
    {
        var admin = await api.SignInAsync();
        var created = await (await admin.PostAsJsonAsync("/api/v1/identity/users",
            new { userName = "audit.reader", email = "audit.reader@example.com", password = "Secure.Pass-2026", mustChangePassword = false }, Ct)).ReadJsonAsync();
        await api.SignInAsync("audit.reader", "Secure.Pass-2026");
        await api.SignInAsync("audit.reader", "Secure.Pass-2026");

        var entries = await WaitForEntriesAsync(admin, $"entityType=identity.user&entityId={created.Id()}", 1);
        await Task.Delay(1500, Ct);
        entries = await ListAsync(admin, $"entityType=identity.user&entityId={created.Id()}");
        entries.Select(e => e!["operation"]!.GetValue<string>()).ShouldBe(["created"]);
        var properties = entries[0]!["changes"]!.AsArray().Select(c => c!["property"]!.GetValue<string>()).ToList();
        properties.ShouldContain("UserName");
        properties.ShouldNotContain("PasswordHash");
        properties.ShouldNotContain("SecurityStamp");

        var reader = await api.SignInAsync("audit.reader", "Secure.Pass-2026");
        (await reader.GetAsync("/api/v1/audit/entries", Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private static async Task<JsonArray> ListAsync(HttpClient client, string query) =>
        (await (await client.GetAsync($"/api/v1/audit/entries?{query}", Ct)).ReadJsonAsync())["items"]!.AsArray();

    /// <summary>History is written by the outbox (every second in the tests).</summary>
    private static async Task<JsonArray> WaitForEntriesAsync(HttpClient client, string query, int expected)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var items = await ListAsync(client, query);
            if (items.Count >= expected)
            {
                return items;
            }

            await Task.Delay(200, Ct);
        }

        throw new TimeoutException($"Expected {expected} audit entries for {query}.");
    }
}
