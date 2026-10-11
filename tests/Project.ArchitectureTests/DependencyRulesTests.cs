using System.Reflection;

namespace Project.ArchitectureTests;

/// <summary>
/// Dependency rules from plan §3.1. Implemented via direct assembly
/// reference walking (no NetArchTest reflection) so we don't have to play
/// with its eNhancedEdition API quirks.
/// </summary>
public sealed class DependencyRulesTests
{
    [Fact]
    public void Application_does_not_depend_on_EF_Core()
    {
        var asm = typeof(Project.Application.PageRequest).Assembly;
        var offenders = asm.GetReferencedAssemblies()
            .Where(a => a.Name?.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase) == true)
            .Select(a => a.Name!)
            .ToArray();

        Assert.True(offenders.Length == 0,
            "Application must not reference Microsoft.EntityFrameworkCore. " +
            "Offenders: " + string.Join(", ", offenders));
    }

    [Fact]
    public void Web_does_not_depend_on_EF_Core_directly()
    {
        var asm = typeof(Project.Web.IModule).Assembly;
        var offenders = asm.GetReferencedAssemblies()
            .Where(a => a.Name?.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase) == true)
            .Select(a => a.Name!)
            .ToArray();

        Assert.True(offenders.Length == 0,
            "Web must not reference Microsoft.EntityFrameworkCore. " +
            "Offenders: " + string.Join(", ", offenders));
    }

    [Fact]
    public void SharedKernel_only_depends_on_BCL_self()
    {
        var asm = typeof(Project.SharedKernel.Result).Assembly;
        var offenders = asm.GetReferencedAssemblies()
            .Where(a => a.Name?.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase) == true
                     || a.Name?.StartsWith("Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase) == true)
            .Select(a => a.Name!)
            .ToArray();

        Assert.True(offenders.Length == 0,
            "SharedKernel must stay free of EF and AspNetCore. " +
            "Offenders: " + string.Join(", ", offenders));
    }
}