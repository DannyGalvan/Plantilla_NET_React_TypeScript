using Microsoft.EntityFrameworkCore;
using Project.Server.Context;

namespace Project.Server.Tests;

/// <summary>
/// Smoke tests that exercise the test infrastructure and ensure the project references
/// resolve cleanly. Phase 1 will add the ownership / IDOR / allowlist / soft-delete tests.
/// </summary>
public class SmokeTests
{
    [Fact]
    public void DataContext_CanBeConstructed_WithInMemoryOptions()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString("N"))
            .Options;

        using var ctx = new DataContext(options);

        Assert.NotNull(ctx);
        Assert.NotNull(ctx.Users);
        Assert.NotNull(ctx.Roles);
        Assert.NotNull(ctx.Operations);
        Assert.NotNull(ctx.RolOperations);
        Assert.NotNull(ctx.LoginAudits);
        Assert.NotNull(ctx.PasswordHistories);
        Assert.NotNull(ctx.Modules);
    }

    [Fact]
    public void InMemoryDatabase_RoundTripsAnEntity()
    {
        var dbName = Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        using (var ctx = new DataContext(options))
        {
            ctx.Modules.Add(new Project.Server.Entities.Models.Module
            {
                Name = "smoke",
                IsVisible = true,
                CreatedBy = 0,
                State = 1
            });
            ctx.SaveChanges();
        }

        using (var ctx = new DataContext(options))
        {
            var module = ctx.Modules.Single();
            Assert.Equal("smoke", module.Name);
        }
    }
}