namespace Project.Server.Configs.Extensions
{
    using Serilog;
    using Serilog.Extensions.Logging;
    using Serilog.Sinks.MSSqlServer;
    using System.Collections.ObjectModel;

    /// <summary>
    /// Defines the <see cref="LogPersistConfiguration" />
    /// </summary>
    public static class LogPersistConfiguration
    {
        /// <summary>
        /// The AddLoggerConfiguration
        /// </summary>
        /// <param name="services">The services<see cref="IServiceCollection"/></param>
        /// <param name="configuration">The configuration<see cref="IConfigurationRoot"/></param>
        /// <returns>The <see cref="IServiceCollection"/></returns>
        public static IServiceCollection AddLoggerConfiguration(this IServiceCollection services, IConfigurationRoot configuration)
        {
            services.AddLogging(loggerBuilder =>
            {
                var columnOptions = new ColumnOptions
                {
                    AdditionalColumns = new Collection<SqlColumn>
                    {
                        new() { ColumnName = "RequestId", DataType = System.Data.SqlDbType.NVarChar, DataLength = 50 }
                    }
                };

                loggerBuilder.AddConfiguration(configuration.GetSection("Serilog"));

                var log = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .WriteTo.MSSqlServer(
                        connectionString: configuration.GetConnectionString("DeliveryApp"),
                        sinkOptions: new MSSqlServerSinkOptions { TableName = "Logs", AutoCreateSqlTable = true },
                        columnOptions: columnOptions,
                        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information)
                    .CreateLogger();

                loggerBuilder.AddProvider(new SerilogLoggerProvider(log));
            });

            return services;
        }
    }
}
