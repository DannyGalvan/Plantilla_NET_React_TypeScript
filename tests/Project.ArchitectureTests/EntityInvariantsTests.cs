using System.Reflection;

namespace Project.ArchitectureTests;

/// <summary>
/// Standalone type-based invariants (no NetArchTest reflection). These guard
/// the small set of cross-module contracts the new architecture depends on.
/// User type is loaded via Reflection so the Identity Mediator.SourceGenerator
/// doesn't fire inside the test project.
/// </summary>
public sealed class EntityInvariantsTests
{
    [Fact]
    public void Product_implements_IMultiTenant()
    {
        var productType = typeof(Project.Modules.Catalog.Domain.Product);
        Assert.True(typeof(Project.SharedKernel.Domain.IMultiTenant).IsAssignableFrom(productType),
            "Product must implement IMultiTenant so the global tenant filter applies.");
    }

    [Fact]
    public void Category_implements_IMultiTenant()
    {
        var categoryType = typeof(Project.Modules.Catalog.Domain.Category);
        Assert.True(typeof(Project.SharedKernel.Domain.IMultiTenant).IsAssignableFrom(categoryType),
            "Category must implement IMultiTenant so the global tenant filter applies.");
    }

    [Fact]
    public void User_implements_IMultiTenant()
    {
        // Identity's Mediator.SourceGenerator conflicts with the test project's
        // transitive Mediator package. We assert the contract by reading the
        // module's compiled assembly directly from its bin folder.
        var identityDll = LocateModuleDll("Identity");
        Assert.True(File.Exists(identityDll),
            "Identity DLL not found at " + identityDll);

        var asm = Assembly.LoadFrom(identityDll);
        var userType = asm.GetType("Project.Modules.Identity.Domain.User");
        var multiTenant = typeof(Project.SharedKernel.Domain.IMultiTenant);
        Assert.NotNull(userType);
        Assert.True(multiTenant.IsAssignableFrom(userType!),
            "User must implement IMultiTenant so the global tenant filter applies.");
    }

    private static string LocateModuleDll(string moduleShort)
    {
        var baseDir = AppContext.BaseDirectory;
        var repoRoot = baseDir;
        for (var i = 0; i < 8; i++)
        {
            var candidate = Path.Combine(repoRoot, "src", "Modules", moduleShort, "bin", "Release", "net10.0",
                "Project.Modules." + moduleShort + ".dll");
            if (File.Exists(candidate)) return candidate;

            repoRoot = Path.GetFullPath(Path.Combine(repoRoot, ".."));
            if (repoRoot == Path.GetPathRoot(repoRoot)) break;
        }

        return Path.Combine(baseDir, "Project.Modules." + moduleShort + ".dll");
    }

    [Fact]
    public void Tenant_implements_IAggregateRoot()
    {
        var tenantType = typeof(Project.Modules.Tenancy.Domain.Tenant);
        Assert.True(typeof(Project.SharedKernel.Domain.AggregateRoot<Guid>).IsAssignableFrom(tenantType),
            "Tenant must be an AggregateRoot so it can raise domain events.");
    }

    [Fact]
    public void Settings_sections_have_distinct_keys()
    {
        Assert.Equal("localization", Project.Modules.Tenancy.Application.LocalizationSettings.SectionKey);
        Assert.Equal("branding", Project.Modules.Tenancy.Application.BrandingSettings.SectionKey);
        Assert.Equal("tax", Project.Modules.Tenancy.Application.TaxSettings.SectionKey);
        Assert.Equal("security", Project.Modules.Tenancy.Application.SecuritySettings.SectionKey);
        Assert.Equal("features", Project.Modules.Tenancy.Application.FeatureSettings.SectionKey);
    }

    [Fact]
    public void Permission_attribute_targets_method_or_class()
    {
        var attrType = typeof(Project.Web.Crud.PermissionAttribute);
        var usage = attrType.GetCustomAttributes(typeof(System.AttributeUsageAttribute), false)
            .Cast<System.AttributeUsageAttribute>()
            .First();

        Assert.True(usage.ValidOn.HasFlag(System.AttributeTargets.Method)
                 || usage.ValidOn.HasFlag(System.AttributeTargets.Class),
            "PermissionAttribute must be usable on methods or classes.");
    }
}