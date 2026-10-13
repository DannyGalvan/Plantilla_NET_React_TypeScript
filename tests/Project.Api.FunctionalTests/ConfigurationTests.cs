using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Project.Api.FunctionalTests.Infrastructure;

namespace Project.Api.FunctionalTests;

/// <summary>Configuration module: values in the platform database, applied without a restart, secrets never returned.</summary>
public sealed class ConfigurationTests(NoneModeApi api) : IClassFixture<NoneModeApi>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task The_jwt_key_is_generated_and_kept_secret()
    {
        var admin = await api.SignInAsync();
        var field = await FieldAsync(admin, "Jwt", "Jwt:SigningKey");
        field["hasValue"]!.GetValue<bool>().ShouldBeTrue();
        field["source"]!.GetValue<string>().ShouldBe("database");
        field["value"].ShouldBeNull();
        field["secret"]!.GetValue<bool>().ShouldBeTrue();
    }

    [Fact]
    public async Task Changes_apply_without_a_restart()
    {
        var admin = await api.SignInAsync();
        (await (await admin.GetAsync("/api/v1/catalog/categories?pageSize=50", Ct)).ReadJsonAsync())["pageSize"]!.GetValue<int>().ShouldBe(50);

        (await admin.PutAsJsonAsync("/api/v1/configuration/Pagination", new { values = new Dictionary<string, object?> { ["Pagination:MaxPageSize"] = 7 } }, Ct))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await (await admin.GetAsync("/api/v1/catalog/categories?pageSize=50", Ct)).ReadJsonAsync())["pageSize"]!.GetValue<int>().ShouldBe(7);
        (await FieldAsync(admin, "Pagination", "Pagination:MaxPageSize"))["source"]!.GetValue<string>().ShouldBe("database");

        // Null removes the value: the default comes back.
        (await admin.PutAsJsonAsync("/api/v1/configuration/Pagination", new { values = new Dictionary<string, object?> { ["Pagination:MaxPageSize"] = null } }, Ct))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await (await admin.GetAsync("/api/v1/catalog/categories?pageSize=50", Ct)).ReadJsonAsync())["pageSize"]!.GetValue<int>().ShouldBe(50);
    }

    [Fact]
    public async Task Secrets_are_stored_but_never_returned()
    {
        var admin = await api.SignInAsync();
        var saved = await admin.PutAsJsonAsync("/api/v1/configuration/Email", new
        {
            values = new Dictionary<string, object?> { ["Email:Host"] = "smtp.example.com", ["Email:Password"] = "smtp-password-123" },
        }, Ct);
        saved.StatusCode.ShouldBe(HttpStatusCode.NoContent, await saved.Content.ReadAsStringAsync(Ct));
        (await admin.PutAsJsonAsync("/api/v1/configuration/Secrets", new
        {
            values = new Dictionary<string, object?> { ["Secrets"] = new Dictionary<string, string> { ["tenant-acme-db"] = "Host=db;Password=very-secret" } },
        }, Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var body = await (await admin.GetAsync("/api/v1/configuration", Ct)).Content.ReadAsStringAsync(Ct);
        body.ShouldNotContain("smtp-password-123");
        body.ShouldNotContain("very-secret");
        (await FieldAsync(admin, "Email", "Email:Host"))["value"]!.GetValue<string>().ShouldBe("smtp.example.com");
        var password = await FieldAsync(admin, "Email", "Email:Password");
        password["hasValue"]!.GetValue<bool>().ShouldBeTrue();
        password["value"].ShouldBeNull();
        var secrets = await FieldAsync(admin, "Secrets", "Secrets");
        secrets["value"]!.AsObject().ContainsKey("tenant-acme-db").ShouldBeTrue();
        secrets["value"]!["tenant-acme-db"].ShouldBeNull();

        (await admin.PutAsJsonAsync("/api/v1/configuration/Email", new { values = new Dictionary<string, object?> { ["Email:Host"] = null, ["Email:Password"] = null } }, Ct))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Invalid_values_and_unknown_keys_are_rejected()
    {
        var admin = await api.SignInAsync();
        foreach (var values in new Dictionary<string, object?>[]
        {
            new() { ["Pagination:MaxPageSize"] = "many" },
            new() { ["Pagination:Unknown"] = 1 },
            new() { ["Database:ConnectionStrings:Default"] = "Host=evil" },
        })
        {
            (await admin.PutAsJsonAsync("/api/v1/configuration/Pagination", new { values }, Ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        (await admin.PutAsJsonAsync("/api/v1/configuration/RateLimiting", new { values = new Dictionary<string, object?> { ["RateLimiting:GlobalPermitPerMinute"] = 0 } }, Ct))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await admin.PutAsJsonAsync("/api/v1/configuration/Database", new { values = new Dictionary<string, object?>() }, Ct))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private static async Task<JsonObject> FieldAsync(HttpClient client, string section, string key)
    {
        var response = await client.GetAsync("/api/v1/configuration", Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(Ct));
        var overview = await response.ReadJsonAsync();
        var fields = overview["sections"]!.AsArray().Single(s => s!["section"]!.GetValue<string>() == section)!["fields"]!.AsArray();
        return fields.Single(f => f!["key"]!.GetValue<string>() == key)!.AsObject();
    }
}
