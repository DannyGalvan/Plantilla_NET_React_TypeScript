using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Options;
using Project.Application.Crud;
using Project.Application.Tenancy;
using Project.Infrastructure.Tenancy;
using Project.SharedKernel;
using Project.Web.Security;

namespace Project.UnitTests.BuildingBlocks;

public sealed class ResultTests
{
    [Fact]
    public void Failure_cannot_expose_a_value()
    {
        Result<int> result = Error.NotFound("x.not_found");
        result.IsFailure.ShouldBeTrue();
        Should.Throw<InvalidOperationException>(() => _ = result.Value);
    }

    [Fact]
    public void Implicit_conversions_build_success_and_failure()
    {
        Result<string> ok = "value";
        ok.Value.ShouldBe("value");
        Result failed = Error.Conflict("x", "y");
        failed.Error!.Type.ShouldBe(ErrorType.Conflict);
    }
}

public sealed class JsonMergePatchTests
{
    [Fact]
    public void Patch_sets_zero_false_and_null_and_keeps_missing_properties()
    {
        var target = JsonNode.Parse("""{"name":"A","stock":5,"isActive":true,"description":"d"}""")!.AsObject();
        JsonMergePatch.Apply(target, JsonNode.Parse("""{"Stock":0,"isActive":false,"description":null}""")!.AsObject());

        target["name"]!.GetValue<string>().ShouldBe("A");
        target["stock"]!.GetValue<int>().ShouldBe(0);
        target["isActive"]!.GetValue<bool>().ShouldBeFalse();
        target["description"].ShouldBeNull();
    }
}

public sealed class TenantContextTests
{
    private static TenantContext Create(TenancyMode mode) => new(Options.Create(new TenancyOptions { Mode = mode }));

    private static TenantInfo Tenant(TenantIsolation isolation) =>
        new(Guid.NewGuid(), "acme", "Acme", TenantStatus.Active, isolation, null, isolation == TenantIsolation.Dedicated ? "acme-db" : null, null, []);

    [Fact]
    public void None_mode_always_has_the_default_tenant_and_no_filter()
    {
        var context = Create(TenancyMode.None);
        context.TenantId.ShouldBe(TenantDefaults.DefaultTenantId);
        context.UsesSharedDatabase.ShouldBeFalse();
        context.UsesDedicatedDatabase.ShouldBeFalse();
    }

    [Theory]
    [InlineData(TenancyMode.SharedDatabase, TenantIsolation.Shared, true, false)]
    [InlineData(TenancyMode.Hybrid, TenantIsolation.Shared, true, false)]
    [InlineData(TenancyMode.Hybrid, TenantIsolation.Dedicated, false, true)]
    [InlineData(TenancyMode.DatabasePerTenant, TenantIsolation.Dedicated, false, true)]
    public void Isolation_depends_on_mode_and_tenant(TenancyMode mode, TenantIsolation isolation, bool shared, bool dedicated)
    {
        var context = Create(mode);
        context.IsResolved.ShouldBeFalse();
        context.Set(Tenant(isolation));
        context.UsesSharedDatabase.ShouldBe(shared);
        context.UsesDedicatedDatabase.ShouldBe(dedicated);
    }

    [Fact]
    public void The_tenant_of_a_scope_cannot_change()
    {
        var context = Create(TenancyMode.SharedDatabase);
        context.Set(Tenant(TenantIsolation.Shared));
        Should.Throw<InvalidOperationException>(() => context.Set(Tenant(TenantIsolation.Shared)));
    }
}

public sealed class EndpointSecurityValidatorTests
{
    private static RouteEndpoint Endpoint(string pattern, params object[] metadata) =>
        new(_ => Task.CompletedTask, RoutePatternFactory.Parse(pattern), 0, new EndpointMetadataCollection(metadata), pattern);

    [Fact]
    public void Api_endpoints_without_a_security_declaration_are_reported()
    {
        var offenders = EndpointSecurityValidator.Validate(
        [
            Endpoint("/api/v1/open"),
            Endpoint("/api/v1/secure", new PermissionMetadata("x.y.read")),
            Endpoint("/api/v1/me", new AuthenticatedOnlyMetadata()),
            Endpoint("/api/v1/login", new Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute()),
            Endpoint("/health/live"),
        ]);

        offenders.ShouldHaveSingleItem().ShouldContain("/api/v1/open");
    }
}
