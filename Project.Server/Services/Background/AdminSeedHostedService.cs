using Microsoft.Extensions.Options;
using Project.Server.Configs.Models;
using Project.Server.Context;
using Project.Server.Entities.Models;
using Project.Server.Services.Core;
using BC = BCrypt.Net;

namespace Project.Server.Services.Background
{
    /// <summary>
    /// Bootstraps the SA seed user with a usable password.
    ///
    /// Closes B6: the seed user used to ship with a hardcoded password hash
    /// that was documented in the source. The new flow:
    /// <list type="number">
    ///   <item><c>UserConfiguration</c> seeds the user with an unusable
    ///         placeholder and <c>MustChangePassword = true</c>.</item>
    ///   <item>This hosted service replaces the placeholder with a BCrypt
    ///         hash of <c>AppSettings:SeedAdminPassword</c>.</item>
    ///   <item>In production the password is supplied via user-secret or
    ///         environment variable. Outside Development the hosted service
    ///         refuses to start when the value is missing.</item>
    /// </list>
    /// </summary>
    public class AdminSeedHostedService : IHostedService
    {
        private const string Placeholder = "!UNUSABLE-SEED!";
        private const long AdminUserId = 1;

        private readonly IServiceProvider _services;
        private readonly IOptions<AppSettings> _appSettings;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<AdminSeedHostedService> _logger;

        public AdminSeedHostedService(
            IServiceProvider services,
            IOptions<AppSettings> appSettings,
            IHostEnvironment environment,
            ILogger<AdminSeedHostedService> logger)
        {
            _services = services;
            _appSettings = appSettings;
            _environment = environment;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var seed = _appSettings.Value.SeedAdminPassword;
            if (string.IsNullOrWhiteSpace(seed))
            {
                if (!_environment.IsDevelopment())
                {
                    throw new InvalidOperationException(
                        "AppSettings:SeedAdminPassword is required outside Development. " +
                        "Configure it via user-secret (development) or environment variable " +
                        "AppSettings__SeedAdminPassword (production).");
                }
                _logger.LogWarning("No SeedAdminPassword configured; skipping SA password bootstrap. " +
                    "Use 'dotnet user-secrets set AppSettings:SeedAdminPassword \"...\"' to set it.");
                return;
            }

            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();

            var user = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .FirstOrDefaultAsync(db.Users, u => u.Id == AdminUserId, cancellationToken);
            if (user is null)
            {
                _logger.LogInformation("SA user not present in database; nothing to bootstrap.");
                return;
            }

            if (!string.Equals(user.Password, Placeholder, StringComparison.Ordinal))
            {
                _logger.LogDebug("SA user already has a real password; skipping bootstrap.");
                return;
            }

            user.Password = BC.BCrypt.HashPassword(seed);
            user.MustChangePassword = true;
            user.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("SA seed password bootstrapped (MustChangePassword=true).");
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}