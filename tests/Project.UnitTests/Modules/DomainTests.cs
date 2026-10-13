using System.Text.Json.Nodes;
using NSubstitute;
using Project.Application.Settings;
using Project.Modules.Identity.Application.Auth;
using Project.Modules.Identity.Domain;
using Project.Modules.Organization.Domain;
using Project.Modules.Tenancy.Application;
using Project.Modules.Tenancy.Domain;

namespace Project.UnitTests.Modules;

public sealed class UserTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Account_locks_after_the_maximum_failed_attempts_and_unlocks_after_the_window()
    {
        var user = User.Create("ana", "ana@example.com", null, "hash", false, Now);

        for (var i = 0; i < 4; i++)
        {
            user.RegisterFailedLogin(5, TimeSpan.FromMinutes(15), Now).ShouldBeFalse();
        }

        user.RegisterFailedLogin(5, TimeSpan.FromMinutes(15), Now).ShouldBeTrue();
        user.IsLockedOut(Now.AddMinutes(1)).ShouldBeTrue();
        user.IsLockedOut(Now.AddMinutes(16)).ShouldBeFalse();
    }

    [Fact]
    public void Changing_the_password_renews_the_security_stamp()
    {
        var user = User.Create("ana", "ana@example.com", null, "hash", true, Now);
        var stamp = user.SecurityStamp;

        user.ChangePassword("new-hash", mustChangePassword: false, Now);

        user.SecurityStamp.ShouldNotBe(stamp);
        user.MustChangePassword.ShouldBeFalse();
    }

    [Fact]
    public void Role_permissions_raise_an_event_only_when_they_change()
    {
        var role = Role.Create("Sales", null, Guid.NewGuid());
        role.SetPermissions(["catalog.products.read"]).ShouldBeTrue();
        role.SetPermissions(["CATALOG.PRODUCTS.READ"]).ShouldBeFalse();
        role.DomainEvents.Count.ShouldBe(1);
    }

    [Fact]
    public void Refresh_tokens_are_single_use()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", Guid.NewGuid(), Guid.NewGuid(), null, Now, TimeSpan.FromDays(1), null);
        token.IsActive(Now).ShouldBeTrue();
        token.Revoke(Now, Guid.NewGuid());
        token.IsActive(Now).ShouldBeFalse();
    }
}

public sealed class PasswordPolicyTests
{
    private static PasswordPolicy Create()
    {
        var settings = Substitute.For<ISettingsProvider>();
        settings.GetAsync<SecuritySettings>(Arg.Any<CancellationToken>()).Returns(new SecuritySettings { PasswordMinLength = 10 });
        return new PasswordPolicy(settings);
    }

    [Theory]
    [InlineData("short1!A")]
    [InlineData("alllowercase1!")]
    [InlineData("NoDigitsHere!!")]
    [InlineData("NoSymbols12345")]
    [InlineData("Contains.ana.2026")]
    public async Task Weak_passwords_are_rejected(string password) =>
        (await Create().ValidateAsync(password, "ana", CancellationToken.None)).ShouldNotBeNull();

    [Fact]
    public async Task Strong_passwords_are_accepted() =>
        (await Create().ValidateAsync("Correct.Horse-42", "ana", CancellationToken.None)).ShouldBeNull();
}

public sealed class OrganizationUnitTests
{
    private static readonly OrganizationUnitType Company = new() { Code = "company", Name = "Company", CanBeRoot = true, AllowedParentCodes = "company" };
    private static readonly OrganizationUnitType Brand = new() { Code = "brand", Name = "Brand", AllowedParentCodes = "company" };

    [Fact]
    public void Paths_materialize_the_hierarchy()
    {
        var root = OrganizationUnit.Create(Company, null, "acme", "Acme", null).Value;
        var brand = OrganizationUnit.Create(Brand, root, "food", "Acme Food", "gt").Value;

        brand.Path.ShouldBe($"/{root.Id:N}/{brand.Id:N}/");
        brand.Depth.ShouldBe(1);
        brand.CountryCode.ShouldBe("GT");
        OrganizationUnit.ParsePath(brand.Path).ShouldBe([root.Id, brand.Id]);
    }

    [Fact]
    public void Hierarchy_rules_are_enforced()
    {
        OrganizationUnit.Create(Brand, null, "x", "X", null).IsFailure.ShouldBeTrue();

        var root = OrganizationUnit.Create(Company, null, "acme", "Acme", null).Value;
        var child = OrganizationUnit.Create(Company, root, "sub", "Sub", null).Value;
        root.MoveTo(Company, child).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Rebase_rewrites_descendant_paths()
    {
        var a = OrganizationUnit.Create(Company, null, "a", "A", null).Value;
        var b = OrganizationUnit.Create(Company, null, "b", "B", null).Value;
        var child = OrganizationUnit.Create(Brand, a, "c", "C", null).Value;

        var oldPath = a.Path;
        a.MoveTo(Company, b).IsSuccess.ShouldBeTrue();
        child.RebasePath(oldPath, a.Path, 1);

        child.Path.ShouldBe($"/{b.Id:N}/{a.Id:N}/{child.Id:N}/");
        child.Depth.ShouldBe(2);
    }
}

public sealed class TenantTests
{
    [Fact]
    public void Domains_are_unique_and_normalized()
    {
        var tenant = Project.Modules.Tenancy.Domain.Tenant.Create("ACME", "Acme", DatabaseIsolation.Shared, null, null, null);
        tenant.Code.ShouldBe("acme");
        tenant.AddDomain("ERP.Acme.com", isPrimary: true).IsSuccess.ShouldBeTrue();
        tenant.AddDomain("erp.acme.com", isPrimary: false).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Dedicated_tenants_start_in_provisioning()
    {
        var tenant = Project.Modules.Tenancy.Domain.Tenant.Create("big", "Big", DatabaseIsolation.Dedicated, "PostgreSql", "big-db", null);
        tenant.Status.ShouldBe(TenantState.Provisioning);
    }

    [Theory]
    [InlineData("#1d4ed8", true)]
    [InlineData("oklch(0.55 0.2 260)", true)]
    [InlineData("red; background:url(x)", false)]
    public void Brand_colors_are_validated(string color, bool valid)
    {
        var section = new SettingsSectionDescriptor(BrandingSettings.SectionKey, typeof(BrandingSettings), false);
        var error = new DefaultSettingsValidator().Validate(section, new JsonObject { ["primaryColor"] = color });
        (error is null).ShouldBe(valid);
    }
}
