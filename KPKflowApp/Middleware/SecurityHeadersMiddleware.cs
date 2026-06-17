using Microsoft.Extensions.Primitives;

namespace KPKflowApp.Middleware
{
    public sealed class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;

        public SecurityHeadersMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
        }

        public Task Invoke(HttpContext context)
        {
            // Remove unwanted headers
            context.Response.Headers.Remove("X-Powered-By");

            // Set security headers
            context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");
            context.Response.Headers.Add("x-content-type-options", "nosniff");
            context.Response.Headers.Add("x-frame-options", "DENY");
            context.Response.Headers.Add("x-xss-protection", "1; mode=block");
            context.Response.Headers.Add("Permissions-Policy", "geolocation=(self)");
            context.Response.Headers.Add("referrer-policy", "strict-origin-when-cross-origin");

            // Determine file type for caching logic
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

            if (path.EndsWith(".js") ||
                path.EndsWith(".css") ||
                path.EndsWith(".jpg") ||
                path.EndsWith(".jpeg") ||
                path.EndsWith(".png") ||
                path.EndsWith(".gif") ||
                path.EndsWith(".svg") ||
                path.EndsWith(".webp") ||
                path.EndsWith(".ico") ||
                path.EndsWith(".woff") ||
                path.EndsWith(".woff2") ||
                path.EndsWith(".ttf") ||
                path.EndsWith(".eot") ||
                path.EndsWith(".otf"))
            {
                //  Cache static assets for 30 days
                context.Response.Headers["Cache-Control"] = "public, max-age=2592000, immutable";
            }
            else
            {
                // Do not cache dynamic content
                context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                context.Response.Headers["Pragma"] = "no-cache";
                context.Response.Headers["Expires"] = "0";
            }

            // (Optional) CSP configuration

            string stylesrcelem = "'sha256-TotaJgQpi3uAcFIYGbWcMXZfL/6yP3V4SMJJLI/gF94=' 'sha256-47DEQpj8HBSa+/TImW+5JCeuQeRkm5NMpJWZG3hSuFU=' 'sha256-2iXqEuHSEQP1S2FDDTYaDeRVL3Q96bRqnCSjZlUqpNw='";
            string stylesrcattr = "'unsafe-hashes' 'sha256-ZdHxw9eWtnxUb3mk6tBS+gIiVUPE3pGM470keHPDFlE=' 'sha256-YwStgWuzr/kqGmgy9cvuUIDSFeyx8ZqmV8CCexCdwo0=' 'sha256-FwG/QEDmtzyDO2c7Mskt+f1QFYU90sJRjn3Y6y9xcAg='";

            context.Response.Headers.Add("Content-Security-Policy", new StringValues(
                "base-uri 'self';" +
                "block-all-mixed-content;" +
                "child-src 'self';" +
                "connect-src 'self' " + _configuration["CONNECT_SRC"] + ";" +
                "default-src 'self';" +

                "font-src 'self' https://fonts.gstatic.com;" +

                "form-action 'self';" +
                "frame-ancestors 'none';" +
                "frame-src 'self';" +
                "img-src 'self' data: blob:;" +
                "manifest-src 'self';" +
                "media-src 'self';" +
                "object-src 'self';" +
                "script-src 'self';" +
                "script-src-attr 'self';" +
                "script-src-elem 'self';" +

                "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com;" +
                "style-src-attr 'self' 'unsafe-inline';" +
                "style-src-elem 'self' 'unsafe-inline' https://fonts.googleapis.com;" +

                "upgrade-insecure-requests;" +
                "worker-src 'self';"
            ));

            return _next(context);
        }
    }
}

