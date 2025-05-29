namespace Project.Server.Configs.Extensions
{
    using Microsoft.OpenApi.Models;

    /// <summary>
    /// Defines the <see cref="SwaggerConfiguration" />
    /// </summary>
    public static class SwaggerConfiguration
    {
        /// <summary>
        /// The AddSwaggerConfiguration
        /// </summary>
        /// <param name="services">The services<see cref="IServiceCollection"/></param>
        public static void AddSwaggerConfiguration(this IServiceCollection services)
        {
            //Add the Swagger configuration
            services.AddEndpointsApiExplorer();

            //Add the Swagger configuration
            services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("bearerAuth", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Encabezado de autorización JWT utilizando el esquema Portador."
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "bearerAuth" }
                        },
                        Array.Empty<string>()
                    }
                });
            });
        }
    }
}
