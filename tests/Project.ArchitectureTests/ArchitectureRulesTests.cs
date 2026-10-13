using System.Reflection;
using NetArchTest.Rules;
using Project.Application.Tenancy;
using Project.Composition;
using Project.SharedKernel.Domain;

namespace Project.ArchitectureTests;

/// <summary>
/// Dependency rules of the modular monolith (docs/05-arquitectura.md). They run on the compiled assemblies, so a
/// forbidden reference fails the build pipeline even if it compiles.
/// </summary>
public sealed class ArchitectureRulesTests
{
    // Every module of the application, discovered from the composition root: a new module is covered automatically.
    private static readonly string[] Modules = [.. AppModules.All().Select(m => m.Name)];

    private static readonly string[] TenantModules = [.. AppModules.All().Where(m => m.DataScope == DataScope.Tenant).Select(m => m.Name)];

    private static Assembly Load(string name) => Assembly.Load(name);

    private static IEnumerable<string> References(Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(a => a.Name!).Where(n => n.StartsWith("Project.", StringComparison.Ordinal) || n.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) || n.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));

    public static TheoryData<string> ModuleNames() => [.. Modules];

    public static TheoryData<string> TenantModuleNames() => [.. TenantModules];

    [Fact]
    public void SharedKernel_has_no_dependencies()
    {
        References(typeof(Entity<>).Assembly).ShouldBeEmpty();
    }

    [Fact]
    public void Application_building_block_does_not_depend_on_infrastructure_or_web_frameworks()
    {
        References(Load("Project.Application")).ShouldBe(["Project.SharedKernel"], ignoreOrder: true);
    }

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Module_domain_only_depends_on_the_shared_kernel(string module)
    {
        References(Load($"Project.Modules.{module}.Domain")).ShouldBe(["Project.SharedKernel"], ignoreOrder: true);
    }

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Module_contracts_only_depend_on_the_shared_kernel(string module)
    {
        References(Load($"Project.Modules.{module}.Contracts")).ShouldAllBe(r => r == "Project.SharedKernel");
    }

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Module_application_has_no_ef_core_aspnet_or_infrastructure(string module)
    {
        var references = References(Load($"Project.Modules.{module}.Application")).ToList();
        references.ShouldNotContain(r => r.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
        references.ShouldNotContain(r => r.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
        references.ShouldNotContain(r => r.EndsWith(".Infrastructure", StringComparison.Ordinal) || r == "Project.Web");
    }

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Modules_only_talk_to_other_modules_through_contracts(string module)
    {
        foreach (var layer in new[] { "Domain", "Application", "Infrastructure" })
        {
            var references = References(Load($"Project.Modules.{module}.{layer}"));
            var foreign = references
                .Where(r => r.StartsWith("Project.Modules.", StringComparison.Ordinal))
                .Where(r => !r.StartsWith($"Project.Modules.{module}.", StringComparison.Ordinal))
                .Where(r => !r.EndsWith(".Contracts", StringComparison.Ordinal))
                .ToList();
            foreign.ShouldBeEmpty($"{module}.{layer} references internals of another module: {string.Join(", ", foreign)}");
        }
    }

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Request_handlers_are_sealed(string module)
    {
        var result = Types.InAssembly(Load($"Project.Modules.{module}.Application"))
            .That().HaveNameEndingWith("Handler")
            .Should().BeSealed()
            .GetResult();
        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? []));
    }

    [Theory]
    [MemberData(nameof(TenantModuleNames))]
    public void Aggregates_of_tenant_modules_are_multi_tenant(string module)
    {
        var offenders = Load($"Project.Modules.{module}.Domain").GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IAggregateRoot).IsAssignableFrom(t))
            .Where(t => !typeof(IMultiTenant).IsAssignableFrom(t))
            .Select(t => t.Name)
            .ToList();
        offenders.ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void DbContexts_are_only_used_by_infrastructure(string module)
    {
        var result = Types.InAssembly(Load($"Project.Modules.{module}.Application"))
            .Should().NotHaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore")
            .GetResult();
        result.IsSuccessful.ShouldBeTrue();
    }
}
