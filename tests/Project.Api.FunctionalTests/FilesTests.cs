using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Project.Api.FunctionalTests.Infrastructure;

namespace Project.Api.FunctionalTests;

/// <summary>Files module: library, attachments with the permissions of their record, storage connections.</summary>
public sealed class FilesTests(NoneModeApi api) : IClassFixture<NoneModeApi>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Library_files_are_uploaded_listed_downloaded_and_deleted()
    {
        var admin = await api.SignInAsync();
        var uploaded = await UploadAsync(admin, "contract.txt", "hello files");
        uploaded.StatusCode.ShouldBe(HttpStatusCode.Created, await uploaded.Content.ReadAsStringAsync(Ct));
        var file = await uploaded.ReadJsonAsync();
        file["provider"]!.GetValue<string>().ShouldBe("local");

        var page = await (await admin.GetAsync("/api/v1/files?search=contract", Ct)).ReadJsonAsync();
        page["items"]!.AsArray().Select(f => f!["id"]!.GetValue<string>()).ShouldContain(file.Id().ToString());

        var download = await admin.GetAsync($"/api/v1/files/{file.Id()}/content", Ct);
        download.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await download.Content.ReadAsStringAsync(Ct)).ShouldBe("hello files");
        download.Content.Headers.ContentDisposition?.DispositionType.ShouldBe("attachment");

        (await admin.DeleteAsync($"/api/v1/files/{file.Id()}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await admin.GetAsync($"/api/v1/files/{file.Id()}/content", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Uploads_are_validated()
    {
        var admin = await api.SignInAsync();
        (await UploadAsync(admin, "script.exe", "MZ")).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await UploadAsync(admin, "note.txt", "x", entityType: "unknown.thing", entityId: Guid.NewGuid().ToString())).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await UploadAsync(admin, "note.txt", "x", entityType: "catalog.product", entityId: Guid.NewGuid().ToString())).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Attachments_follow_the_permissions_and_scope_of_their_record()
    {
        var admin = await api.SignInAsync();
        var root = (await (await admin.GetAsync("/api/v1/organization/units", Ct)).ReadArrayAsync()).First(u => u!["parentId"] is null)!.AsObject();
        var brandA = await (await admin.PostAsJsonAsync("/api/v1/organization/units", new { typeCode = "brand", parentId = root.Id(), code = "files-a", name = "Files A" }, Ct)).ReadJsonAsync();
        var brandB = await (await admin.PostAsJsonAsync("/api/v1/organization/units", new { typeCode = "brand", parentId = root.Id(), code = "files-b", name = "Files B" }, Ct)).ReadJsonAsync();
        var category = await (await admin.PostAsJsonAsync("/api/v1/catalog/categories", new { name = "Files" }, Ct)).ReadJsonAsync();
        var productA = await (await admin.PostAsJsonAsync("/api/v1/catalog/products",
            new { sku = "FILE-A", name = "A", price = 1, stock = 1, categoryId = category.Id(), organizationUnitId = brandA.Id() }, Ct)).ReadJsonAsync();
        var productB = await (await admin.PostAsJsonAsync("/api/v1/catalog/products",
            new { sku = "FILE-B", name = "B", price = 1, stock = 1, categoryId = category.Id(), organizationUnitId = brandB.Id() }, Ct)).ReadJsonAsync();

        var photoA = await (await UploadAsync(admin, "a.png", "png-a", "catalog.product", productA.Id().ToString())).ReadJsonAsync();
        var photoB = await (await UploadAsync(admin, "b.png", "png-b", "catalog.product", productB.Id().ToString())).ReadJsonAsync();

        var roles = await (await admin.GetAsync("/api/v1/identity/roles?filter=name:eq:User", Ct)).ReadJsonAsync();
        var user = await (await admin.PostAsJsonAsync("/api/v1/identity/users",
            new { userName = "files.user", email = "files.user@example.com", password = "Secure.Pass-2026", roleIds = new[] { roles["items"]![0]!.AsObject().Id() }, mustChangePassword = false }, Ct)).ReadJsonAsync();
        (await admin.PostAsJsonAsync($"/api/v1/organization/units/{brandA.Id()}/users", new { userId = user.Id() }, Ct)).EnsureSuccessStatusCode();
        var brandUser = await api.SignInAsync("files.user", "Secure.Pass-2026");

        var listA = await brandUser.GetAsync($"/api/v1/files?entityType=catalog.product&entityId={productA.Id()}", Ct);
        listA.StatusCode.ShouldBe(HttpStatusCode.OK, await listA.Content.ReadAsStringAsync(Ct));
        (await listA.ReadJsonAsync())["items"]!.AsArray().Count.ShouldBe(1);
        (await brandUser.GetAsync($"/api/v1/files/{photoA.Id()}/content", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);

        // The product of another brand does not exist for this user, and neither do its files.
        (await brandUser.GetAsync($"/api/v1/files?entityType=catalog.product&entityId={productB.Id()}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await brandUser.GetAsync($"/api/v1/files/{photoB.Id()}/content", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // Readers can browse the library, but uploading and storage administration need their own permissions.
        (await brandUser.GetAsync("/api/v1/files", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await UploadAsync(brandUser, "denied.txt", "x")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await UploadAsync(brandUser, "denied.png", "x", "catalog.product", productA.Id().ToString())).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await brandUser.GetAsync("/api/v1/files/storage/connections", Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Storage_connections_never_expose_credentials_nor_reach_custom_endpoints()
    {
        var admin = await api.SignInAsync();
        var providers = await (await admin.GetAsync("/api/v1/files/storage/providers", Ct)).ReadArrayAsync();
        providers.Select(p => p!["provider"]!.GetValue<string>()).ShouldBe(["azure-blob", "google-drive", "onedrive", "s3"]);
        providers.Single(p => p!["provider"]!.GetValue<string>() == "google-drive")!["available"]!.GetValue<bool>().ShouldBeFalse();

        var ssrf = await admin.PostAsJsonAsync("/api/v1/files/storage/connections", new
        {
            name = "internal",
            provider = "s3",
            settings = new { bucket = "b", serviceUrl = "http://169.254.169.254" },
            credentials = new { accessKeyId = "a", secretAccessKey = "s" },
        }, Ct);
        ssrf.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var created = await admin.PostAsJsonAsync("/api/v1/files/storage/connections", new
        {
            name = "Archive",
            provider = "s3",
            settings = new { bucket = "archive", region = "us-east-1" },
            credentials = new { accessKeyId = "AKIAEXAMPLE", secretAccessKey = "not-a-real-secret" },
            isDefault = false,
        }, Ct);
        created.StatusCode.ShouldBe(HttpStatusCode.OK, await created.Content.ReadAsStringAsync(Ct));
        var body = await created.Content.ReadAsStringAsync(Ct);
        body.ShouldNotContain("not-a-real-secret");
        body.ShouldNotContain("AKIAEXAMPLE");

        // Google is not configured on this platform; a forged callback state is rejected.
        (await admin.PostAsJsonAsync("/api/v1/files/storage/oauth/google-drive/start", new { name = "Drive", isDefault = true }, Ct))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await api.Client().GetAsync("/api/v1/files/oauth/callback?code=x&state=forged", Ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var id = (await created.ReadJsonAsync()).Id();
        (await admin.DeleteAsync($"/api/v1/files/storage/connections/{id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    internal static Task<HttpResponseMessage> UploadAsync(HttpClient client, string name, string text, string? entityType = null, string? entityId = null)
    {
        var form = new MultipartFormDataContent();
        var content = new ByteArrayContent(Encoding.UTF8.GetBytes(text));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(content, "file", name);
        if (entityType is not null)
        {
            form.Add(new StringContent(entityType), "entityType");
            form.Add(new StringContent(entityId ?? string.Empty), "entityId");
        }

        return client.PostAsync("/api/v1/files", form, Ct);
    }
}
