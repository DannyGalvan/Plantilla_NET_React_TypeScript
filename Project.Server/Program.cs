using Project.Server.Configs.Extensions;
using Project.Server.Configs.Models;

namespace Project.Server
{
    public abstract class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Obtain the environment current (Development, Production, etc.)
            string environment = builder.Environment.EnvironmentName;

            // Configure the ConfigurationBuilder y load the configurations del archive appsettings.json
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true) // Load base file appsettings.json
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true) // Load environment-specific file
                .AddEnvironmentVariables()
                .Build();

            //Add the configuration to the builder
            IConfigurationSection appSettingsSection = configuration.GetSection("AppSettings");

            AppSettings appSettingsConfig = appSettingsSection.Get<AppSettings>()!;

            builder.Services.Configure<AppSettings>(appSettingsSection);

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddMapsterSettings();
            builder.Services.AddJwtConfiguration(appSettingsConfig);
            builder.Services.AddSwaggerConfiguration();
            builder.Services.AddContextGroup(configuration);
            builder.Services.AddValidationsGroup();
            builder.Services.AddServiceGroup();
            builder.Services.AddControllersConfiguration();
            //builder.Services.AddLoggerConfiguration(configuration);

            var app = builder.Build();

            app.UseDefaultFiles();
            app.UseStaticFiles();

            // Configure the HTTP request pipeline.
            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.MapControllers();

            app.MapFallbackToFile("/index.html");

            app.Run();
        }
    }
}
