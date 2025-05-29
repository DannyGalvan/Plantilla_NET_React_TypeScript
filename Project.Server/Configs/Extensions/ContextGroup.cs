namespace Project.Server.Configs.Extensions
{
    using Microsoft.EntityFrameworkCore;
    using Project.Server.Context;

    /// <summary>
    /// Defines the <see cref="ContextGroup" />
    /// </summary>
    public static class ContextGroup
    {
        /// <summary>
        /// The AddContextGroup
        /// </summary>
        /// <param name="services">The services<see cref="IServiceCollection"/></param>
        /// <param name="configuration">The configuration<see cref="IConfiguration"/></param>
        /// <returns>The <see cref="IServiceCollection"/></returns>
        public static IServiceCollection AddContextGroup(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<DataContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("EsiSchoolPayments"));
            });

            return services;
        }
    }
}
