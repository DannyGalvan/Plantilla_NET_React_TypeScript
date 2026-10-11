using System.Reflection;

namespace Project.ArchitectureTests;

/// <summary>
/// Enforces the "no physical deletes" rule (Fase 5 deferred item) plus a
/// few invariants. Module loading happens by Assembly.Load on the compiled
/// DLL so the test project doesn't pull Mediator.SourceGenerator and other
/// transitive source generators.
/// </summary>
public sealed class SoftDeleteRulesTests
{
    [Fact]
    public void No_business_module_depends_on_EF_Core()
    {
        var assemblyNames = new[]
        {
            "Project.Modules.Identity",
            "Project.Modules.Catalog",
            "Project.Modules.Organization",
            "Project.Modules.Tenancy",
        };

        var offenders = new List<string>();
        foreach (var assemblyName in assemblyNames)
        {
            try
            {
                var asm = Assembly.Load(assemblyName);
                var efs = asm.GetReferencedAssemblies()
                    .Where(a => a.Name?.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase) == true)
                    .Select(a => a.Name!)
                    .ToArray();
                if (efs.Length > 0)
                {
                    offenders.AddRange(efs);
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                // Module not built into bin yet; skip.
            }
        }

        Assert.True(offenders.Count == 0,
            "Modules must not reference EF Core directly. " +
            "Offenders: " + string.Join(", ", offenders));
    }
}