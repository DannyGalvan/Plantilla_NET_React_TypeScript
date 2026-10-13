using System.Security.Cryptography;
using System.Text;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Project.Modules.Identity.Application.Auth;
using Project.Modules.Identity.Application.Me;
using Project.SharedKernel;
using Project.Web;
using Project.Web.Http;
using Project.Web.Security;

namespace Project.Modules.Identity.Infrastructure.Presentation;

public sealed record LoginRequest(string UserName, string Password, string? TenantCode);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(string Token, string NewPassword);

public sealed record SwitchContextRequest(Guid TenantId, Guid? OrganizationUnitId);

/// <summary>Body returned with every new session. The refresh token travels only in an HttpOnly cookie.</summary>
public sealed record SessionResponse(string AccessToken, DateTimeOffset ExpiresAt, bool MustChangePassword, Guid TenantId, Guid? OrganizationUnitId);

/// <summary>
/// Writes the session cookies. The refresh token is HttpOnly, SameSite=Strict, Secure outside Development and scoped
/// to the auth path. A readable <c>XSRF-TOKEN</c> cookie implements double-submit CSRF protection for refresh/logout.
/// </summary>
internal static class AuthCookies
{
    public const string RefreshCookie = "rt";
    public const string XsrfCookie = "XSRF-TOKEN";
    public const string XsrfHeader = "X-XSRF-TOKEN";
    public const string RefreshPath = "/api/v1/identity/auth";

    public static IResult WriteSession(HttpContext http, AuthResult session)
    {
        var secure = http.Request.IsHttps || !http.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment();
        http.Response.Cookies.Append(RefreshCookie, session.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Strict,
            Path = RefreshPath,
            Expires = session.RefreshTokenExpiresAt,
            IsEssential = true,
        });
        http.Response.Cookies.Append(XsrfCookie, Convert.ToHexString(RandomNumberGenerator.GetBytes(32)), new CookieOptions
        {
            HttpOnly = false,
            Secure = secure,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = session.RefreshTokenExpiresAt,
            IsEssential = true,
        });

        return TypedResults.Ok(new SessionResponse(session.AccessToken, session.AccessTokenExpiresAt, session.MustChangePassword, session.TenantId, session.OrganizationUnitId));
    }

    public static void Clear(HttpContext http)
    {
        http.Response.Cookies.Delete(RefreshCookie, new CookieOptions { Path = RefreshPath });
        http.Response.Cookies.Delete(XsrfCookie, new CookieOptions { Path = "/" });
    }

    public static bool IsCsrfValid(HttpContext http)
    {
        var cookie = http.Request.Cookies[XsrfCookie];
        var header = http.Request.Headers[XsrfHeader].ToString();
        return !string.IsNullOrEmpty(cookie)
            && !string.IsNullOrEmpty(header)
            && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(cookie), Encoding.UTF8.GetBytes(header));
    }
}

internal static class AuthEndpoints
{
    public static void Map(IEndpointRouteBuilder api)
    {
        var auth = api.MapGroup("identity/auth").WithTags("auth");

        auth.MapPost("/login", async (LoginRequest request, ISender sender, HttpContext http, CancellationToken ct) =>
            {
                var result = await sender.Send(new LoginCommand(request.UserName, request.Password, request.TenantCode), ct);
                return result.IsSuccess ? AuthCookies.WriteSession(http, result.Value) : result.Error!.ToProblem();
            })
            .AllowAnonymous()
            .AllowWithoutTenant()
            .RequireRateLimiting(PlatformExtensions.AuthRateLimitPolicy)
            .WithName("Login");

        auth.MapPost("/refresh", async (ISender sender, HttpContext http, CancellationToken ct) =>
            {
                if (!AuthCookies.IsCsrfValid(http))
                {
                    return Error.Forbidden("auth.csrf", "The CSRF token is missing or invalid.").ToProblem();
                }

                var result = await sender.Send(new RefreshSessionCommand(http.Request.Cookies[AuthCookies.RefreshCookie] ?? string.Empty), ct);
                if (result.IsFailure)
                {
                    AuthCookies.Clear(http);
                    return result.Error!.ToProblem();
                }

                return AuthCookies.WriteSession(http, result.Value);
            })
            .AllowAnonymous()
            .AllowWithoutTenant()
            .WithName("RefreshSession");

        auth.MapPost("/logout", async (ISender sender, HttpContext http, CancellationToken ct) =>
            {
                if (!AuthCookies.IsCsrfValid(http))
                {
                    return Error.Forbidden("auth.csrf", "The CSRF token is missing or invalid.").ToProblem();
                }

                await sender.Send(new LogoutCommand(http.Request.Cookies[AuthCookies.RefreshCookie]), ct);
                AuthCookies.Clear(http);
                return TypedResults.NoContent();
            })
            .AllowAnonymous()
            .AllowWithoutTenant()
            .WithName("Logout");

        auth.MapPost("/forgot-password", async (ForgotPasswordRequest request, ISender sender, CancellationToken ct) =>
                (await sender.Send(new ForgotPasswordCommand(request.Email), ct)).ToHttp())
            .AllowAnonymous()
            .AllowWithoutTenant()
            .RequireRateLimiting(PlatformExtensions.AuthRateLimitPolicy)
            .WithName("ForgotPassword");

        auth.MapPost("/reset-password", async (ResetPasswordRequest request, ISender sender, CancellationToken ct) =>
                (await sender.Send(new ResetPasswordCommand(request.Token, request.NewPassword), ct)).ToHttp())
            .AllowAnonymous()
            .AllowWithoutTenant()
            .RequireRateLimiting(PlatformExtensions.AuthRateLimitPolicy)
            .WithName("ResetPassword");

        auth.MapPost("/change-password", async (ChangePasswordRequest request, ISender sender, HttpContext http, CancellationToken ct) =>
            {
                var result = await sender.Send(new ChangePasswordCommand(request.CurrentPassword, request.NewPassword), ct);
                return result.IsSuccess ? AuthCookies.WriteSession(http, result.Value) : result.Error!.ToProblem();
            })
            .RequireAuthenticatedUser()
            .WithName("ChangePassword");

        auth.MapPost("/switch-context", async (SwitchContextRequest request, ISender sender, HttpContext http, CancellationToken ct) =>
            {
                var result = await sender.Send(new SwitchContextCommand(request.TenantId, request.OrganizationUnitId), ct);
                return result.IsSuccess ? AuthCookies.WriteSession(http, result.Value) : result.Error!.ToProblem();
            })
            .RequireAuthenticatedUser()
            .WithName("SwitchContext");

        var me = api.MapGroup("me").WithTags("me");
        me.MapGet("/", async (ISender sender, CancellationToken ct) => (await sender.Send(new GetProfileQuery(), ct)).ToHttp())
            .RequireAuthenticatedUser()
            .WithName("GetProfile");
        me.MapGet("/navigation", async (ISender sender, CancellationToken ct) => (await sender.Send(new GetNavigationQuery(), ct)).ToHttp())
            .RequireAuthenticatedUser()
            .WithName("GetNavigation");
    }
}
