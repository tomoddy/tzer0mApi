namespace tzer0mApi.Services.Middleware
{
    /// <summary>
    /// Api key middleware extentions
    /// </summary>
    public static class ApiKeyMiddlewareExtensions
    {
        /// <summary>
        /// Add api key middleware to the application builder
        /// </summary>
        /// <param name="builder">Application builder</param>
        /// <returns>Application builder</returns>
        public static IApplicationBuilder UseApiKeyMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ApiKeyMiddleware>();
        }
    }
}