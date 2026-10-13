using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Project.Infrastructure.Services;
using Project.Modules.Files.Application;
using Project.Modules.Files.Infrastructure.Storage;

namespace Project.UnitTests.Modules.Files;

public sealed class CloudStorageTests
{
    private static readonly StoredObject Small = new(Guid.CreateVersion7(), Guid.CreateVersion7(), "report.pdf", "application/pdf", 5, DateTimeOffset.UtcNow);

    private static IOptions<FilesOptions> Options(bool allowCustom = false) => Microsoft.Extensions.Options.Options.Create(new FilesOptions
    {
        AllowCustomEndpoints = allowCustom,
        OAuth = new OAuthOptions
        {
            Google = new OAuthClientOptions { ClientId = "google-client", ClientSecret = "google-secret" },
            Microsoft = new OAuthClientOptions { ClientId = "ms-client", ClientSecret = "ms-secret" },
        },
    });

    [Fact]
    public async Task OneDrive_stores_the_rotated_refresh_token_and_uploads_large_files_in_chunks()
    {
        var http = new FakeHttp();
        http.On("login.microsoftonline.com", _ => Json(new { access_token = "at-1", expires_in = 3600, refresh_token = "rt-2" }));
        http.On("createUploadSession", _ => Json(new { uploadUrl = "https://upload.example.com/session" }));
        http.On("upload.example.com", request =>
        {
            var range = request.Content!.Headers.ContentRange!;
            return range.To + 1 == range.Length ? Json(new { id = "item-1" }, HttpStatusCode.Created) : new HttpResponseMessage(HttpStatusCode.Accepted);
        });

        IReadOnlyDictionary<string, string>? persisted = null;
        var account = new StorageAccount(Guid.CreateVersion7(), "onedrive", new Dictionary<string, string>(), new Dictionary<string, string> { ["refresh_token"] = "rt-1" })
        {
            OnCredentialsChanged = (credentials, _) =>
            {
                persisted = credentials;
                return Task.CompletedTask;
            },
        };

        var storage = new OneDriveStorage(http, new MemoryCache(new MemoryCacheOptions()), TimeProvider.System, Options());
        var length = (25 * 1024 * 1024) + 7;
        var file = Small with { Length = length, FileName = "backup.zip" };

        var location = await storage.SaveAsync(account, file, new MemoryStream(new byte[length]), TestContext.Current.CancellationToken);

        location.ShouldBe("item-1");
        persisted!["refresh_token"].ShouldBe("rt-2");
        var chunks = http.Requests.Where(r => r.RequestUri!.Host == "upload.example.com").ToList();
        chunks.Count.ShouldBe(3);
        chunks.ShouldAllBe(r => r.Headers.Authorization == null);
        chunks[^1].Content!.Headers.ContentRange!.From.ShouldBe(20L * 1024 * 1024);
    }

    [Fact]
    public async Task Google_drive_uses_a_resumable_session_and_caches_the_access_token()
    {
        var http = new FakeHttp();
        http.On("oauth2.googleapis.com", _ => Json(new { access_token = "at", expires_in = 3600 }));
        http.On("uploadType=resumable", _ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.Location = new Uri("https://www.googleapis.com/upload/session-1");
            return response;
        });
        http.On("session-1", _ => Json(new { id = "drive-file" }));

        var storage = new GoogleDriveStorage(http, new MemoryCache(new MemoryCacheOptions()), TimeProvider.System, Options());
        var account = new StorageAccount(Guid.CreateVersion7(), "google-drive", new Dictionary<string, string> { ["folderId"] = "folder" }, new Dictionary<string, string> { ["refresh_token"] = "rt" });

        (await storage.SaveAsync(account, Small, new MemoryStream("hello"u8.ToArray()), TestContext.Current.CancellationToken)).ShouldBe("drive-file");
        (await storage.SaveAsync(account, Small, new MemoryStream("hello"u8.ToArray()), TestContext.Current.CancellationToken)).ShouldBe("drive-file");

        http.Requests.Count(r => r.RequestUri!.Host == "oauth2.googleapis.com").ShouldBe(1);
        var metadata = await http.Bodies[1].ShouldNotBeNull().ReadAsStringAsync(TestContext.Current.CancellationToken);
        metadata.ShouldContain("\"parents\":[\"folder\"]");
        http.Requests[2].Content!.Headers.ContentLength.ShouldBe(5);
    }

    [Fact]
    public async Task Google_service_accounts_sign_a_jwt_assertion()
    {
        using var rsa = RSA.Create(2048);
        var http = new FakeHttp();
        http.On("oauth2.googleapis.com", _ => Json(new { access_token = "at", expires_in = 3600 }));
        http.On("drive/v3/files", _ => new HttpResponseMessage(HttpStatusCode.NoContent));

        var storage = new GoogleDriveStorage(http, new MemoryCache(new MemoryCacheOptions()), TimeProvider.System, Options());
        var account = new StorageAccount(null, "google-drive", new Dictionary<string, string>(), new Dictionary<string, string>
        {
            ["client_email"] = "robot@project.iam.gserviceaccount.com",
            ["private_key"] = rsa.ExportPkcs8PrivateKeyPem(),
        });

        await storage.DeleteAsync(account, "file-id", TestContext.Current.CancellationToken);

        var form = await http.Bodies[0].ShouldNotBeNull().ReadAsStringAsync(TestContext.Current.CancellationToken);
        form.ShouldContain("grant_type=urn%3Aietf%3Aparams%3Aoauth%3Agrant-type%3Ajwt-bearer");
        var assertion = Uri.UnescapeDataString(form.Split("assertion=")[1]).Split('.');
        assertion.Length.ShouldBe(3);
        var signature = Convert.FromBase64String(Pad(assertion[2]));
        rsa.VerifyData(Encoding.ASCII.GetBytes($"{assertion[0]}.{assertion[1]}"), signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1).ShouldBeTrue();
    }

    [Fact]
    public void Tenant_connections_cannot_target_internal_endpoints()
    {
        var s3 = new S3FileStorage(Options());
        var keys = new Dictionary<string, string> { ["accessKeyId"] = "a", ["secretAccessKey"] = "s" };
        s3.Validate(new Dictionary<string, string> { ["bucket"] = "b", ["serviceUrl"] = "http://10.0.0.5:9000" }, keys).ShouldNotBeNull();
        s3.Validate(new Dictionary<string, string> { ["bucket"] = "b", ["region"] = "us-east-1" }, keys).ShouldBeNull();
        new S3FileStorage(Options(allowCustom: true)).Validate(new Dictionary<string, string> { ["bucket"] = "b", ["serviceUrl"] = "http://minio:9000" }, keys).ShouldBeNull();

        var azure = new AzureBlobFileStorage(Options());
        azure.Validate(new Dictionary<string, string> { ["container"] = "c" }, new Dictionary<string, string>
        {
            ["connectionString"] = "DefaultEndpointsProtocol=http;AccountName=a;AccountKey=a2V5;BlobEndpoint=http://169.254.169.254/a;",
        }).ShouldNotBeNull();
    }

    [Fact]
    public void OAuth_states_cannot_be_forged_or_reused_after_they_expire()
    {
        var clock = new ManualClock(DateTimeOffset.UtcNow);
        var protector = new DataProtectionSecretProtector(new EphemeralDataProtectionProvider());
        var reader = new OAuthStateReader(protector, clock);
        var state = new OAuthState(Guid.CreateVersion7(), "user", "google-drive", "Drive", "https://app/cb", "https://app/files/storage", true, clock.GetUtcNow().AddMinutes(15));
        var token = protector.Protect("files.oauth-state", System.Text.Json.JsonSerializer.Serialize(state));

        reader.Read(token).ShouldBe(state);
        reader.Read(token[..^4] + "AAAA").ShouldBeNull();
        reader.Read("forged").ShouldBeNull();
        clock.Now = clock.Now.AddMinutes(16);
        reader.Read(token).ShouldBeNull();
    }

    private static string Pad(string base64Url)
    {
        var value = base64Url.Replace('-', '+').Replace('_', '/');
        return value.PadRight(value.Length + ((4 - (value.Length % 4)) % 4), '=');
    }

    private static HttpResponseMessage Json(object body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = JsonContent.Create(body) };

    private sealed class ManualClock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }

    /// <summary>Scripted HTTP endpoints matched by a fragment of the URL; keeps every request and a copy of its body.</summary>
    private sealed class FakeHttp : HttpMessageHandler, IHttpClientFactory
    {
        private readonly List<(string Fragment, Func<HttpRequestMessage, HttpResponseMessage> Respond)> _routes = [];

        public List<HttpRequestMessage> Requests { get; } = [];

        public List<HttpContent?> Bodies { get; } = [];

        public void On(string fragment, Func<HttpRequestMessage, HttpResponseMessage> respond) => _routes.Add((fragment, respond));

        public HttpClient CreateClient(string name) => new(this, disposeHandler: false);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            if (request.Content is null)
            {
                Bodies.Add(null);
            }
            else
            {
                var bytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
                var copy = new ByteArrayContent(bytes);
                foreach (var header in request.Content.Headers)
                {
                    copy.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                Bodies.Add(copy);
            }

            var url = request.RequestUri!.ToString();
            var route = _routes.LastOrDefault(r => url.Contains(r.Fragment, StringComparison.Ordinal));
            return route.Respond?.Invoke(request) ?? new HttpResponseMessage(HttpStatusCode.NotFound) { Content = JsonContent.Create(new JsonObject()) };
        }
    }
}
