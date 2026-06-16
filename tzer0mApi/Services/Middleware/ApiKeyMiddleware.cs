using Microsoft.Extensions.Primitives;
using System.Security.Cryptography;
using System.Text;
using tzer0mApi.Services.Keys;

namespace tzer0mApi.Services.Middleware
{
    /// <summary>
    /// Api key middleware
    /// </summary>
    /// <param name="next">Task to continue on completion</param>
    public class ApiKeyMiddleware(RequestDelegate next)
    {
        // Store the next delegate in the pipeline
        private readonly RequestDelegate _next = next;

        // Header name
        private const string KEY_HEADER_NAME = "X-API-Key";

        /// <summary>
        /// Checks the request for a valid API key if the path is private, otherwise continues to the next middleware
        /// </summary>
        /// <param name="context">Request context</param>
        /// <param name="configuration">Configuration</param>
        /// <returns>Task</returns>
        public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
        {
            // Get private paths from configuration and check if valid
            List<string>? privatePaths = configuration.GetSection("Authentication:PrivatePaths").Get<List<string>>();
            if (privatePaths == null)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("Private paths are not configured");
                return;
            }

            // Check request path is valid
            if (context.Request.Path.Value == null)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid request path");
                return;
            }

            // Check if request path is private, if not skip authentication check
            string requestPath = context.Request.Path.Value.ToLowerInvariant();
            if (!privatePaths.Any(p => p.EndsWith("/*") ? requestPath.StartsWith(p[..^2], StringComparison.InvariantCultureIgnoreCase) : requestPath.Equals(p, StringComparison.InvariantCultureIgnoreCase)))
            {
                await _next(context);
                return;
            }

            // Check if API key header is present
            if (!context.Request.Headers.TryGetValue(KEY_HEADER_NAME, out StringValues extractedKey))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("API key is missing");
                return;
            }

            // Hash the extracted key
            byte[] extractedKeyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(extractedKey.ToString()));
            string hashedExtractedKey = Convert.ToBase64String(extractedKeyBytes);

            // Validate against database
            KeysService keysService = context.RequestServices.GetRequiredService<KeysService>();
            if (!await keysService.IsValidKeyAsync(hashedExtractedKey))
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Unauthorized client");
                return;
            }

            // Key is valid, continue
            await _next(context);
        }
    }
}