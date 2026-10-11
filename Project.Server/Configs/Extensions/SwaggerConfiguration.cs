namespace Project.Server.Configs.Extensions
{
    /// <summary>
    /// Defines the <see cref="SwaggerConfiguration" />.
    /// Swashbuckle has been removed in this phase. The new OpenAPI document is
    /// exposed by <c>Project.Api</c> via <c>Microsoft.AspNetCore.OpenApi</c>.
    /// </summary>
    public static class SwaggerConfiguration
    {
        public static void AddSwaggerConfiguration(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
        }
    }
}