using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Project.Api.FunctionalTests.Infrastructure;
using Project.Modules.Webhooks.Infrastructure;

namespace Project.Api.FunctionalTests;

/// <summary>Webhooks module: SSRF guard, signed deliveries of module events, failures, retries.</summary>
public sealed class WebhooksTests(NoneModeApi api) : IClassFixture<NoneModeApi>
{
    private static readonly string[] AllEvents = ["*"];
    private static readonly string[] UnknownEvents = ["nope"];
    private static readonly string[] RecordEvents = ["records.changed"];

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Module_events_reach_the_endpoints_signed_and_retried()
    {
        var admin = await api.SignInAsync();
        var events = await (await admin.GetAsync("/api/v1/webhooks/events", Ct)).ReadArrayAsync();
        events.Select(e => e!["name"]!.GetValue<string>()).ShouldContain("records.changed");

        foreach (var url in new[] { "http://hooks.example.com/in", "https://127.0.0.1/in", "https://10.1.2.3/in", "https://user:pass@hooks.example.com/in", "not a url" })
        {
            var rejected = await admin.PostAsJsonAsync("/api/v1/webhooks/subscriptions", new { name = "bad", url, events = AllEvents }, Ct);
            rejected.StatusCode.ShouldBe(HttpStatusCode.BadRequest, url);
        }

        (await admin.PostAsJsonAsync("/api/v1/webhooks/subscriptions", new { name = "bad", url = "https://hooks.example.com/in", events = UnknownEvents }, Ct))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var created = await (await admin.PostAsJsonAsync("/api/v1/webhooks/subscriptions",
            new { name = "ERP", url = "https://hooks.example.com/erp", events = RecordEvents }, Ct)).ReadJsonAsync();
        var secret = created["secret"]!.GetValue<string>();
        secret.ShouldStartWith("whsec_");
        var subscriptionId = created["subscription"]!.AsObject().Id();
        (await (await admin.GetAsync("/api/v1/webhooks/subscriptions", Ct)).Content.ReadAsStringAsync(Ct)).ShouldNotContain(secret);

        // A change in Catalog travels: catalog outbox -> audit event -> webhook delivery -> endpoint.
        await admin.PostAsJsonAsync("/api/v1/catalog/categories", new { name = "Webhook category" }, Ct);
        var call = await api.WebhookCalls.WaitForAsync(c => c.Body.Contains("Webhook category", StringComparison.Ordinal), Ct);
        call.Url.ShouldBe("https://hooks.example.com/erp");
        call.Headers["X-Webhook-Event"].ShouldBe("records.changed");
        var signature = call.Headers["X-Webhook-Signature"].Split(',');
        var timestamp = signature[0]["t=".Length..];
        signature[1].ShouldBe($"v1={WebhookSender.Sign(secret, timestamp, call.Body)}");
        var body = JsonNode.Parse(call.Body)!;
        body["event"]!.GetValue<string>().ShouldBe("records.changed");
        body["data"]!["entityType"]!.GetValue<string>().ShouldBe("catalog.category");

        // A failing endpoint keeps the delivery pending with the error; a manual retry delivers it.
        api.WebhookCalls.Status = HttpStatusCode.InternalServerError;
        (await admin.PostAsync($"/api/v1/webhooks/subscriptions/{subscriptionId}/ping", null, Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var failed = await WaitForDeliveryAsync(admin, subscriptionId, d => d["event"]!.GetValue<string>() == "webhooks.ping" && d["attempts"]!.GetValue<int>() == 1);
        failed["status"]!.GetValue<string>().ShouldBe("Pending");
        failed["responseStatus"]!.GetValue<int>().ShouldBe(500);

        api.WebhookCalls.Status = HttpStatusCode.OK;
        (await admin.PostAsync($"/api/v1/webhooks/deliveries/{failed.Id()}/retry", null, Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await WaitForDeliveryAsync(admin, subscriptionId, d => d["id"]!.GetValue<string>() == failed.Id().ToString() && d["status"]!.GetValue<string>() == "Delivered");
    }

    private static async Task<JsonObject> WaitForDeliveryAsync(HttpClient client, Guid subscriptionId, Func<JsonObject, bool> match)
    {
        for (var attempt = 0; attempt < 75; attempt++)
        {
            var deliveries = await (await client.GetAsync($"/api/v1/webhooks/subscriptions/{subscriptionId}/deliveries", Ct)).ReadArrayAsync();
            if (deliveries.OfType<JsonObject>().FirstOrDefault(match) is { } found)
            {
                return found;
            }

            await Task.Delay(200, Ct);
        }

        throw new TimeoutException("The delivery did not reach the expected state.");
    }
}
