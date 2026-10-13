using System.Net;
using System.Net.Http.Json;
using Project.Api.FunctionalTests.Infrastructure;

namespace Project.Api.FunctionalTests;

/// <summary>Notifications module: inbox per user, email through the outbox and templates customized per tenant.</summary>
public sealed class NotificationsTests(NoneModeApi api) : IClassFixture<NoneModeApi>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Notifications_reach_the_inbox_and_the_email_of_the_user_only()
    {
        var admin = await api.SignInAsync();
        await admin.PostAsync("/api/v1/me/notifications/read-all", null, Ct);

        var customized = await admin.PutAsJsonAsync("/api/v1/notifications/templates/notifications.test/es",
            new { subject = "Custom subject for {{user}}", body = "Line one\n<script>alert(1)</script>" }, Ct);
        customized.StatusCode.ShouldBe(HttpStatusCode.NoContent, await customized.Content.ReadAsStringAsync(Ct));
        (await admin.PostAsync("/api/v1/notifications/test", null, Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var count = await (await admin.GetAsync("/api/v1/me/notifications/unread-count", Ct)).ReadJsonAsync();
        count["count"]!.GetValue<int>().ShouldBe(1);
        var inbox = await (await admin.GetAsync("/api/v1/me/notifications?unreadOnly=true", Ct)).ReadJsonAsync();
        var entry = inbox["items"]![0]!.AsObject();
        entry["title"]!.GetValue<string>().ShouldBe("Custom subject for admin");

        // The email copy is delivered by the outbox; data and template text are HTML encoded.
        var email = await api.Emails.WaitForAsync(m => m.Subject == "Custom subject for admin", Ct);
        email.Body.ShouldContain("&lt;script&gt;");
        email.Body.ShouldNotContain("<script>");

        // Another user does not see it.
        var reader = await CreateReaderAsync(admin, "inbox.reader");
        var other = await (await reader.GetAsync("/api/v1/me/notifications/unread-count", Ct)).ReadJsonAsync();
        other["count"]!.GetValue<int>().ShouldBe(0);
        (await reader.PostAsync($"/api/v1/me/notifications/{entry.Id()}/read", null, Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await (await admin.GetAsync("/api/v1/me/notifications/unread-count", Ct)).ReadJsonAsync())["count"]!.GetValue<int>().ShouldBe(1);

        (await admin.PostAsync($"/api/v1/me/notifications/{entry.Id()}/read", null, Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await (await admin.GetAsync("/api/v1/me/notifications/unread-count", Ct)).ReadJsonAsync())["count"]!.GetValue<int>().ShouldBe(0);

        // Empty subject and body restore the default text.
        (await admin.PutAsJsonAsync("/api/v1/notifications/templates/notifications.test/es", new { subject = "", body = "" }, Ct)).EnsureSuccessStatusCode();
        var templates = await (await admin.GetAsync("/api/v1/notifications/templates", Ct)).ReadArrayAsync();
        templates.Single(t => t!["key"]!.GetValue<string>() == "notifications.test" && t["language"]!.GetValue<string>() == "es")!["customized"]!
            .GetValue<bool>().ShouldBeFalse();
        (await reader.GetAsync("/api/v1/notifications/templates", Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private async Task<HttpClient> CreateReaderAsync(HttpClient admin, string userName)
    {
        var roles = await (await admin.GetAsync("/api/v1/identity/roles?filter=name:eq:User", Ct)).ReadJsonAsync();
        (await admin.PostAsJsonAsync("/api/v1/identity/users",
            new { userName, email = $"{userName}@example.com", password = "Secure.Pass-2026", roleIds = new[] { roles["items"]![0]!.AsObject().Id() }, mustChangePassword = false }, Ct))
            .EnsureSuccessStatusCode();
        return await api.SignInAsync(userName, "Secure.Pass-2026");
    }
}
