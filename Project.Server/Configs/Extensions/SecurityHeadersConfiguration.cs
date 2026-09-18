using Microsoft.Net.Http.Headers;

namespace Project.Server.Configs.Extensions
{
    /// <summary>
    /// Security response headers. Closes OWASP A05 (Security Misconfiguration).
    ///
    /// CSP is set to <c>default-src 'self'</c>; the SPA bundles its scripts at
    /// build time, so no <c>unsafe-inline</c> is required. If a future module
    /// needs inline content, prefer a nonce + per-request hash.
    /// </summary>
    public static class SecurityHeadersConfiguration
    {
        public const string CspHeader =
            "default-src 'self'; " +
            "script-src 'self'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data:; " +
            "font-src 'self' data:; " +
            "connect-src 'self'; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'";

        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                var headers = context.Response.Headers;

                headers[HeaderNames.ContentSecurityPolicy] = CspHeader;
                headers[HeaderNames.XContentTypeOptions] = "nosniff";
                headers[HeaderNames.XFrameOptions] = "DENY";
                headers["Referrer-Policy"] = "no-referrer";
                headers["Permissions-Policy"] =
                    "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()";
                headers["Cross-Origin-Opener-Policy"] = "same-origin";
                headers["Cross-Origin-Resource-Policy"] = "same-origin";

                await next();
            });
        }
    }
}