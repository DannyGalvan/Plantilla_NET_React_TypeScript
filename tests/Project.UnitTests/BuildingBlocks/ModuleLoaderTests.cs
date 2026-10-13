using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Project.Application.Tenancy;
using Project.Web.Modules;

namespace Project.UnitTests.BuildingBlocks;

public sealed class ModuleLoaderTests
{
    [Fact]
    public void Opt_in_modules_load_only_when_enabled_in_configuration()
    {
        LoadedModules().ShouldBe(["Core"]);
        LoadedModules(("Modules:Enabled:0", "Sample")).ShouldBe(["Core", "Sample"]);
    }

    [Fact]
    public void Disabled_wins_over_enabled()
    {
        LoadedModules(("Modules:Enabled:0", "Sample"), ("Modules:Disabled:0", "sample"), ("Modules:Disabled:1", "Core"))
            .ShouldBeEmpty();
    }

    private static string[] LoadedModules(params (string Key, string Value)[] settings)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
        builder.Configuration.AddInMemoryCollection(settings.Select(s => KeyValuePair.Create(s.Key, (string?)s.Value)));
        builder.AddModules(new FakeModule("Core", true), new FakeModule("Sample", false));
        using var provider = builder.Services.BuildServiceProvider();
        return provider.GetRequiredService<ModuleCatalog>().Modules.Select(m => m.Name).ToArray();
    }

    private sealed class FakeModule(string name, bool enabledByDefault) : IModule
    {
        public string Name => name;

        public DataScope DataScope => DataScope.Tenant;

        public bool EnabledByDefault => enabledByDefault;

        public void AddServices(IServiceCollection services, IConfiguration configuration)
        {
        }

        public void MapEndpoints(IEndpointRouteBuilder api)
        {
        }
    }
}
