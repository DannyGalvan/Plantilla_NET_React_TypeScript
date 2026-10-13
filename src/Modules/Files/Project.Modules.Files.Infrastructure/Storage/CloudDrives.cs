using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Project.Modules.Files.Application;
using Project.Modules.Files.Contracts;

namespace Project.Modules.Files.Infrastructure.Storage;

/// <summary>Access tokens of cloud drives, cached in memory per account until shortly before they expire.</summary>
public abstract class CloudDriveStorage(IHttpClientFactory httpFactory, IMemoryCache cache, TimeProvider clock)
{
    public const string HttpClientName = "files.cloud";

    protected HttpClient Http => httpFactory.CreateClient(HttpClientName);

    protected TimeProvider Clock => clock;

    protected async Task<string> AccessTokenAsync(StorageAccount account, Func<StorageAccount, CancellationToken, Task<TokenResponse>> acquire, CancellationToken ct)
    {
        var key = $"files:token:{GetType().Name}:{account.ConnectionId?.ToString("N") ?? "platform"}";
        if (cache.TryGetValue(key, out string? token) && token is not null)
        {
            return token;
        }

        var response = await acquire(account, ct);
        cache.Set(key, response.AccessToken, TimeSpan.FromSeconds(Math.Max(60, response.ExpiresIn - 120)));
        return response.AccessToken;
    }

    protected async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, string accessToken, CancellationToken ct, HttpCompletionOption completion = HttpCompletionOption.ResponseContentRead)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await Http.SendAsync(request, completion, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            response.Dispose();
            throw new HttpRequestException($"{request.Method} {request.RequestUri?.GetLeftPart(UriPartial.Path)} failed with {(int)response.StatusCode}: {Truncate(body)}", null, response.StatusCode);
        }

        return response;
    }

    protected async Task<TokenResponse> PostTokenAsync(string url, IEnumerable<KeyValuePair<string, string>> form, CancellationToken ct)
    {
        using var response = await Http.PostAsync(url, new FormUrlEncodedContent(form), ct);
        var json = await response.Content.ReadFromJsonAsync<JsonObject>(ct) ?? [];
        if (!response.IsSuccessStatusCode || json["access_token"] is null)
        {
            throw new HttpRequestException($"The token endpoint rejected the request: {json["error"]} {json["error_description"]}", null, response.StatusCode);
        }

        return new TokenResponse(
            json["access_token"]!.GetValue<string>(),
            json["expires_in"]?.GetValue<int>() ?? 3600,
            json["refresh_token"]?.GetValue<string>(),
            json["id_token"]?.GetValue<string>());
    }

    protected static Dictionary<string, string> With(IReadOnlyDictionary<string, string> source, string key, string value) =>
        new(source) { [key] = value };

    /// <summary>Reads a claim of a JWT received directly from the token endpoint over TLS (no signature check needed).</summary>
    protected static string? Claim(string? jwt, string name)
    {
        var parts = jwt?.Split('.');
        if (parts is not { Length: 3 })
        {
            return null;
        }

        var payload = parts[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');
        var json = JsonNode.Parse(Convert.FromBase64String(payload));
        return json?[name]?.GetValue<string>();
    }

    private static string Truncate(string value) => value.Length > 300 ? value[..300] : value;

    protected sealed record TokenResponse(string AccessToken, int ExpiresIn, string? RefreshToken, string? IdToken);
}

/// <summary>
/// Google Drive through the REST API. Two ways to connect: OAuth consent of a user (refresh token, scope
/// <c>drive.file</c>: the app only sees the files it created) or a service account with access to a shared drive.
/// </summary>
public sealed class GoogleDriveStorage(IHttpClientFactory httpFactory, IMemoryCache cache, TimeProvider clock, IOptions<FilesOptions> options)
    : CloudDriveStorage(httpFactory, cache, clock), IFileStorage, IOAuthStorageProvider
{
    private const string TokenUrl = "https://oauth2.googleapis.com/token";
    private const string Api = "https://www.googleapis.com";
    private const string FileScope = "https://www.googleapis.com/auth/drive.file";

    private OAuthClientOptions Client => options.Value.OAuth.Google;

    public string Provider => StorageProviders.GoogleDrive;

    public bool AllowsTenantConnections => true;

    public bool IsConfigured => Client.IsConfigured;

    public string? Validate(IReadOnlyDictionary<string, string> settings, IReadOnlyDictionary<string, string> credentials) =>
        credentials.ContainsKey("client_email") && credentials.ContainsKey("private_key")
            ? null
            : "Connect Google Drive with the consent screen, or provide the client_email and private_key of a service account.";

    public string BuildAuthorizationUrl(string state, string redirectUri) =>
        "https://accounts.google.com/o/oauth2/v2/auth?" + Query(
            ("client_id", Client.ClientId!),
            ("redirect_uri", redirectUri),
            ("response_type", "code"),
            ("scope", $"openid email {FileScope}"),
            ("access_type", "offline"),
            ("prompt", "consent"),
            ("state", state));

    public async Task<OAuthGrant> ExchangeCodeAsync(string code, string redirectUri, CancellationToken ct)
    {
        var token = await PostTokenAsync(TokenUrl, Form(
            ("grant_type", "authorization_code"),
            ("code", code),
            ("client_id", Client.ClientId!),
            ("client_secret", Client.ClientSecret!),
            ("redirect_uri", redirectUri)), ct);
        if (token.RefreshToken is null)
        {
            throw new InvalidOperationException("Google did not return a refresh token.");
        }

        return new OAuthGrant(new Dictionary<string, string> { ["refresh_token"] = token.RefreshToken }, Claim(token.IdToken, "email"));
    }

    public async Task<string> SaveAsync(StorageAccount account, StoredObject file, Stream content, CancellationToken ct)
    {
        var token = await AccessTokenAsync(account, AcquireAsync, ct);
        var metadata = new JsonObject { ["name"] = file.FileName, ["mimeType"] = file.ContentType };
        if (account.Setting("folderId") is { } folder)
        {
            metadata["parents"] = new JsonArray(folder);
        }

        // Resumable upload: one session request with the metadata, then the content in a single PUT.
        using var start = new HttpRequestMessage(HttpMethod.Post, $"{Api}/upload/drive/v3/files?uploadType=resumable&supportsAllDrives=true")
        {
            Content = JsonContent.Create(metadata),
        };
        start.Headers.Add("X-Upload-Content-Type", file.ContentType);
        start.Headers.Add("X-Upload-Content-Length", file.Length.ToString(CultureInfo.InvariantCulture));
        using var session = await SendAsync(start, token, ct);
        var uploadUrl = session.Headers.Location ?? throw new InvalidOperationException("Google Drive did not return an upload session.");

        using var put = new HttpRequestMessage(HttpMethod.Put, uploadUrl) { Content = new StreamContent(content) };
        put.Content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        put.Content.Headers.ContentLength = file.Length;
        using var uploaded = await SendAsync(put, token, ct);
        var json = await uploaded.Content.ReadFromJsonAsync<JsonObject>(ct);
        return json?["id"]?.GetValue<string>() ?? throw new InvalidOperationException("Google Drive did not return the file id.");
    }

    public async Task<Stream> OpenReadAsync(StorageAccount account, string location, CancellationToken ct)
    {
        var token = await AccessTokenAsync(account, AcquireAsync, ct);
        var request = new HttpRequestMessage(HttpMethod.Get, $"{Api}/drive/v3/files/{Uri.EscapeDataString(location)}?alt=media&supportsAllDrives=true");
        var response = await SendAsync(request, token, ct, HttpCompletionOption.ResponseHeadersRead);
        return new OwnedStream(await response.Content.ReadAsStreamAsync(ct), response, request);
    }

    public async Task DeleteAsync(StorageAccount account, string location, CancellationToken ct)
    {
        var token = await AccessTokenAsync(account, AcquireAsync, ct);
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"{Api}/drive/v3/files/{Uri.EscapeDataString(location)}?supportsAllDrives=true");
        try
        {
            using var _ = await SendAsync(request, token, ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Already gone.
        }
    }

    private Task<TokenResponse> AcquireAsync(StorageAccount account, CancellationToken ct)
    {
        if (account.Credential("refresh_token") is { } refresh)
        {
            return PostTokenAsync(TokenUrl, Form(
                ("grant_type", "refresh_token"),
                ("refresh_token", refresh),
                ("client_id", Client.ClientId ?? string.Empty),
                ("client_secret", Client.ClientSecret ?? string.Empty)), ct);
        }

        // Service account: a signed JWT assertion exchanged for an access token (RFC 7523).
        var email = account.Credential("client_email") ?? throw new InvalidOperationException("The Google connection has no credentials.");
        var now = Clock.GetUtcNow().ToUnixTimeSeconds();
        var header = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new { alg = "RS256", typ = "JWT" }));
        var claims = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new
        {
            iss = email,
            scope = "https://www.googleapis.com/auth/drive",
            aud = TokenUrl,
            iat = now,
            exp = now + 3600,
        }));
        using var rsa = RSA.Create();
        rsa.ImportFromPem(account.Credential("private_key")!.Replace("\\n", "\n", StringComparison.Ordinal));
        var signature = Base64Url(rsa.SignData(Encoding.ASCII.GetBytes($"{header}.{claims}"), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        return PostTokenAsync(TokenUrl, Form(
            ("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"),
            ("assertion", $"{header}.{claims}.{signature}")), ct);
    }

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    internal static string Query(params (string Key, string Value)[] values) =>
        string.Join('&', values.Select(v => $"{v.Key}={Uri.EscapeDataString(v.Value)}"));

    internal static IEnumerable<KeyValuePair<string, string>> Form(params (string Key, string Value)[] values) =>
        values.Select(v => KeyValuePair.Create(v.Key, v.Value));
}

/// <summary>
/// OneDrive and SharePoint document libraries through Microsoft Graph. Connected with the consent screen of a user
/// (refresh token, rotated on every refresh and stored again) or with an app registration (client credentials) and
/// a <c>driveId</c>. Files above 4 MB use an upload session in 10 MB chunks.
/// </summary>
public sealed class OneDriveStorage(IHttpClientFactory httpFactory, IMemoryCache cache, TimeProvider clock, IOptions<FilesOptions> options)
    : CloudDriveStorage(httpFactory, cache, clock), IFileStorage, IOAuthStorageProvider
{
    private const string Graph = "https://graph.microsoft.com/v1.0";
    private const string DelegatedScopes = "offline_access openid email User.Read Files.ReadWrite";
    private const long SimpleUploadLimit = 4 * 1024 * 1024;
    private const int ChunkSize = 10 * 1024 * 1024; // Multiple of 320 KiB, as Graph requires.

    private OAuthClientOptions Client => options.Value.OAuth.Microsoft;

    public string Provider => StorageProviders.OneDrive;

    public bool AllowsTenantConnections => true;

    public bool IsConfigured => Client.IsConfigured;

    public string? Validate(IReadOnlyDictionary<string, string> settings, IReadOnlyDictionary<string, string> credentials) =>
        credentials.ContainsKey("tenant_id") && credentials.ContainsKey("client_id") && credentials.ContainsKey("client_secret") && settings.ContainsKey("driveId")
            ? null
            : "Connect OneDrive with the consent screen, or provide tenant_id, client_id, client_secret and the driveId setting.";

    public string BuildAuthorizationUrl(string state, string redirectUri) =>
        $"{Authority(Client.Tenant)}/authorize?" + GoogleDriveStorage.Query(
            ("client_id", Client.ClientId!),
            ("response_type", "code"),
            ("redirect_uri", redirectUri),
            ("response_mode", "query"),
            ("scope", DelegatedScopes),
            ("prompt", "select_account"),
            ("state", state));

    public async Task<OAuthGrant> ExchangeCodeAsync(string code, string redirectUri, CancellationToken ct)
    {
        var token = await PostTokenAsync($"{Authority(Client.Tenant)}/token", GoogleDriveStorage.Form(
            ("grant_type", "authorization_code"),
            ("code", code),
            ("client_id", Client.ClientId!),
            ("client_secret", Client.ClientSecret!),
            ("redirect_uri", redirectUri),
            ("scope", DelegatedScopes)), ct);
        if (token.RefreshToken is null)
        {
            throw new InvalidOperationException("Microsoft did not return a refresh token.");
        }

        var account = Claim(token.IdToken, "email") ?? Claim(token.IdToken, "preferred_username");
        return new OAuthGrant(new Dictionary<string, string> { ["refresh_token"] = token.RefreshToken }, account);
    }

    public async Task<string> SaveAsync(StorageAccount account, StoredObject file, Stream content, CancellationToken ct)
    {
        var token = await AccessTokenAsync(account, AcquireAsync, ct);
        var folder = (account.Setting("folder") ?? "Project files").Trim('/');
        var name = $"{file.Id:N}-{SafeName(file.FileName)}";
        var path = $"{DriveRoot(account)}/root:/{Uri.EscapeDataString(folder)}/{Uri.EscapeDataString(name)}";

        if (file.Length <= SimpleUploadLimit)
        {
            using var put = new HttpRequestMessage(HttpMethod.Put, $"{path}:/content") { Content = new StreamContent(content) };
            put.Content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            using var response = await SendAsync(put, token, ct);
            return await ItemIdAsync(response, ct);
        }

        using var create = new HttpRequestMessage(HttpMethod.Post, $"{path}:/createUploadSession")
        {
            Content = JsonContent.Create(new JsonObject { ["item"] = new JsonObject { ["@microsoft.graph.conflictBehavior"] = "rename" } }),
        };
        using var session = await SendAsync(create, token, ct);
        var uploadUrl = (await session.Content.ReadFromJsonAsync<JsonObject>(ct))?["uploadUrl"]?.GetValue<string>()
            ?? throw new InvalidOperationException("OneDrive did not return an upload session.");

        // The upload URL is pre-authenticated: chunks are sent without the bearer token.
        var buffer = new byte[ChunkSize];
        long offset = 0;
        while (offset < file.Length)
        {
            var read = await content.ReadAtLeastAsync(buffer, (int)Math.Min(ChunkSize, file.Length - offset), throwOnEndOfStream: true, ct);
            using var chunk = new HttpRequestMessage(HttpMethod.Put, uploadUrl) { Content = new ByteArrayContent(buffer, 0, read) };
            chunk.Content.Headers.ContentRange = new ContentRangeHeaderValue(offset, offset + read - 1, file.Length);
            using var response = await Http.SendAsync(chunk, ct);
            response.EnsureSuccessStatusCode();
            offset += read;
            if (response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created)
            {
                return await ItemIdAsync(response, ct);
            }
        }

        throw new InvalidOperationException("The OneDrive upload session ended without an item.");
    }

    public async Task<Stream> OpenReadAsync(StorageAccount account, string location, CancellationToken ct)
    {
        var token = await AccessTokenAsync(account, AcquireAsync, ct);
        var request = new HttpRequestMessage(HttpMethod.Get, $"{DriveRoot(account)}/items/{Uri.EscapeDataString(location)}/content");
        var response = await SendAsync(request, token, ct, HttpCompletionOption.ResponseHeadersRead);
        return new OwnedStream(await response.Content.ReadAsStreamAsync(ct), response, request);
    }

    public async Task DeleteAsync(StorageAccount account, string location, CancellationToken ct)
    {
        var token = await AccessTokenAsync(account, AcquireAsync, ct);
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"{DriveRoot(account)}/items/{Uri.EscapeDataString(location)}");
        try
        {
            using var _ = await SendAsync(request, token, ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Already gone.
        }
    }

    private async Task<TokenResponse> AcquireAsync(StorageAccount account, CancellationToken ct)
    {
        if (account.Credential("refresh_token") is { } refresh)
        {
            var token = await PostTokenAsync($"{Authority(Client.Tenant)}/token", GoogleDriveStorage.Form(
                ("grant_type", "refresh_token"),
                ("refresh_token", refresh),
                ("client_id", Client.ClientId ?? string.Empty),
                ("client_secret", Client.ClientSecret ?? string.Empty),
                ("scope", DelegatedScopes)), ct);
            if (token.RefreshToken is { } rotated && rotated != refresh)
            {
                await account.UpdateCredentialsAsync(With(account.Credentials, "refresh_token", rotated), ct);
            }

            return token;
        }

        return await PostTokenAsync($"{Authority(account.Credential("tenant_id")!)}/token", GoogleDriveStorage.Form(
            ("grant_type", "client_credentials"),
            ("client_id", account.Credential("client_id")!),
            ("client_secret", account.Credential("client_secret")!),
            ("scope", "https://graph.microsoft.com/.default")), ct);
    }

    private static string Authority(string tenant) => $"https://login.microsoftonline.com/{Uri.EscapeDataString(tenant)}/oauth2/v2.0";

    private static string DriveRoot(StorageAccount account) =>
        account.Setting("driveId") is { } drive ? $"{Graph}/drives/{Uri.EscapeDataString(drive)}" : $"{Graph}/me/drive";

    private static string SafeName(string name) =>
        string.Concat(name.Select(c => Path.GetInvalidFileNameChars().Contains(c) || c is '#' or '%' or ':' or '*' or '?' ? '_' : c));

    private static async Task<string> ItemIdAsync(HttpResponseMessage response, CancellationToken ct) =>
        (await response.Content.ReadFromJsonAsync<JsonObject>(ct))?["id"]?.GetValue<string>()
        ?? throw new InvalidOperationException("OneDrive did not return the item id.");
}
