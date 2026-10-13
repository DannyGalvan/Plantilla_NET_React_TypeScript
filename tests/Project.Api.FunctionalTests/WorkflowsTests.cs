using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Project.Api.FunctionalTests.Infrastructure;
using Project.Application.Tenancy;
using Project.Application.Workflows;

namespace Project.Api.FunctionalTests;

/// <summary>Workflows module: steps by permission and role, conditional by amount, separation of duties, outcome events.</summary>
public sealed class WorkflowsTests(NoneModeApi api) : IClassFixture<NoneModeApi>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Requests_move_through_their_steps_and_tell_the_owning_module()
    {
        var admin = await api.SignInAsync();
        var managers = await CreateRoleAsync(admin, "Purchase managers", ["catalog.products.update"]);
        var finance = await CreateRoleAsync(admin, "Finance", []);
        var requester = await CreateUserAsync(admin, "wf.requester", null);
        await CreateUserAsync(admin, "wf.manager", managers);
        var cfo = await CreateUserAsync(admin, "wf.cfo", finance);

        var entityId = Guid.NewGuid().ToString();
        var requestId = (await SubmitAsync(new ApprovalSubmission("tests.purchase", "tests.order", entityId, "Laptops for sales", requester, 5000m, "/orders"))).ShouldNotBeNull();
        (await SubmitAsync(new ApprovalSubmission("tests.purchase", "tests.order", entityId, "Duplicate", requester, 5000m))).ShouldBeNull();

        // The requester cannot decide on his own request.
        var requesterClient = await api.SignInAsync("wf.requester", "Secure.Pass-2026");
        (await requesterClient.PostAsJsonAsync($"/api/v1/workflows/requests/{requestId}/approve", new { comment = "self" }, Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // Step 1: anyone with the permission.
        var manager = await api.SignInAsync("wf.manager", "Secure.Pass-2026");
        var inbox = await WaitForInboxAsync(manager, requestId);
        inbox["steps"]!.AsArray().Count.ShouldBe(2);
        (await manager.PostAsJsonAsync($"/api/v1/workflows/requests/{requestId}/reject", new { comment = "" }, Ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await manager.PostAsJsonAsync($"/api/v1/workflows/requests/{requestId}/approve", new { comment = "Budget ok" }, Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);

        // Step 2 (amount >= 1000): the role Finance, notified through the inbox.
        var cfoClient = await api.SignInAsync("wf.cfo", "Secure.Pass-2026");
        await WaitForInboxAsync(cfoClient, requestId);
        await WaitForNotificationAsync(cfoClient, "Laptops for sales");
        var approved = await (await cfoClient.PostAsJsonAsync($"/api/v1/workflows/requests/{requestId}/approve", new { comment = (string?)null }, Ct)).ReadJsonAsync();
        approved["status"]!.GetValue<string>().ShouldBe("Approved");

        for (var attempt = 0; attempt < 50 && !api.Approvals.Any(a => a.RequestId == requestId); attempt++)
        {
            await Task.Delay(200, Ct);
        }

        var outcome = api.Approvals.Single(a => a.RequestId == requestId);
        outcome.Status.ShouldBe(ApprovalStatus.Approved);
        outcome.EntityId.ShouldBe(entityId);
        await api.Emails.WaitForAsync(m => m.To == "wf.requester@example.com" && m.Subject.Contains("Laptops for sales", StringComparison.Ordinal), Ct);

        // Small purchases skip the conditional step.
        var small = (await SubmitAsync(new ApprovalSubmission("tests.purchase", "tests.order", Guid.NewGuid().ToString(), "Mouse", requester, 20m))).ShouldNotBeNull();
        var mine = await (await requesterClient.GetAsync("/api/v1/workflows/requests/mine", Ct)).ReadArrayAsync();
        mine.Single(r => r!["id"]!.GetValue<string>() == small.ToString())!["steps"]!.AsArray().Count.ShouldBe(1);
        (await requesterClient.PostAsync($"/api/v1/workflows/requests/{small}/cancel", null, Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);

        // Tenants replace the steps of a flow.
        var update = await admin.PutAsJsonAsync("/api/v1/workflows/definitions/tests.purchase",
            new { steps = new[] { new { name = "CFO", kind = "User", approver = cfo.ToString() } } }, Ct);
        update.StatusCode.ShouldBe(HttpStatusCode.NoContent, await update.Content.ReadAsStringAsync(Ct));
        var definition = (await (await admin.GetAsync("/api/v1/workflows/definitions", Ct)).ReadArrayAsync()).Single(d => d!["key"]!.GetValue<string>() == "tests.purchase")!;
        definition["customized"]!.GetValue<bool>().ShouldBeTrue();
        (await requesterClient.GetAsync("/api/v1/workflows/definitions", Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private async Task<Guid?> SubmitAsync(ApprovalSubmission submission)
    {
        Guid? id = null;
        await api.Services.GetRequiredService<ITenantJobRunner>().RunForTenantAsync(TenantDefaults.DefaultTenantId, async (services, _, token) =>
        {
            var result = await services.GetRequiredService<IApprovalService>().SubmitAsync(submission, token);
            id = result.IsSuccess ? result.Value : null;
        }, Ct);
        return id;
    }

    private static async Task<JsonObject> WaitForInboxAsync(HttpClient client, Guid requestId)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var inbox = await (await client.GetAsync("/api/v1/workflows/requests/inbox", Ct)).ReadArrayAsync();
            if (inbox.FirstOrDefault(r => r!["id"]!.GetValue<string>() == requestId.ToString()) is JsonObject found)
            {
                return found;
            }

            await Task.Delay(200, Ct);
        }

        throw new TimeoutException("The request did not reach the inbox.");
    }

    private static async Task WaitForNotificationAsync(HttpClient client, string text)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var inbox = await (await client.GetAsync("/api/v1/me/notifications", Ct)).ReadJsonAsync();
            if (inbox["items"]!.AsArray().Any(n => n!["title"]!.GetValue<string>().Contains(text, StringComparison.Ordinal)))
            {
                return;
            }

            await Task.Delay(200, Ct);
        }

        throw new TimeoutException("The approver was not notified.");
    }

    private static async Task<Guid> CreateRoleAsync(HttpClient admin, string name, string[] permissions) =>
        (await (await admin.PostAsJsonAsync("/api/v1/identity/roles", new { name, permissions }, Ct)).ReadJsonAsync()).Id();

    private static async Task<Guid> CreateUserAsync(HttpClient admin, string userName, Guid? role)
    {
        var response = await admin.PostAsJsonAsync("/api/v1/identity/users", new
        {
            userName,
            email = $"{userName}@example.com",
            password = "Secure.Pass-2026",
            roleIds = role is null ? Array.Empty<Guid>() : [role.Value],
            mustChangePassword = false,
        }, Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(Ct));
        return (await response.ReadJsonAsync()).Id();
    }
}
