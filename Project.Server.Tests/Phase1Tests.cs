using System.Reflection;
using FluentValidation;
using FluentValidation.Results;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Project.Server.Configs.Models;
using Project.Server.Context;
using Project.Server.Entities.Models;
using Project.Server.Entities.Request;
using Project.Server.Entities.Response;
using Project.Server.Interceptors.Interfaces;
using Project.Server.Mappers;
using Project.Server.Services.Core;
using Project.Server.Services.Interfaces;
using Project.Server.Utils;

namespace Project.Server.Tests;

/// <summary>
/// Phase 1 acceptance tests. Each test maps to a finding in the plan.
/// </summary>
public class Phase1Tests
{
    private const long UserAId = 1001;
    private const long UserBId = 2002;

    static Phase1Tests()
    {
        // Mapster mappings are registered globally on a static config. Calling this
        // once keeps the EntityService mapper working in tests.
        MapsterConfig.RegisterMappings();
    }

    // ---------------------------------------------------------------------
    // B1 — IDOR: cross-tenant access must return Forbidden.
    // ---------------------------------------------------------------------

    [Fact]
    public async Task GetByIdOwn_ForeignRecord_ReturnsForbidden()
    {
        var (svc, db, current) = await BuildSeededAsync();
        long foreignId = await SeedUserAsync(db, ownerId: UserBId);

        current.UserId = UserAId;
        var response = await svc.GetByIdOwnAsync(foreignId);

        Assert.False(response.Success);
        Assert.Equal(ResponseStatus.Forbidden, response.Status);
    }

    [Fact]
    public async Task GetAllOwn_NeverReturnsOtherUsersRows()
    {
        var (svc, db, current) = await BuildSeededAsync();
        await SeedUserAsync(db, ownerId: UserAId, name: "A-row");
        await SeedUserAsync(db, ownerId: UserBId, name: "B-row");

        current.UserId = UserAId;
        var response = await svc.GetAllOwnAsync(new QueryOptions());

        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Single(response.Data);
        Assert.All(response.Data, u => Assert.Equal(UserAId, u.CreatedBy));
    }

    // ---------------------------------------------------------------------
    // B2 — Mass assignment: RolId in PUT body must be ignored.
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Update_WithRolIdInBody_DoesNotChangeRole()
    {
        var (svc, db, current) = await BuildSeededAsync();
        long userId = await SeedUserAsync(db, ownerId: UserAId, rolId: 2);

        var request = new UserRequest
        {
            Id = userId,
            RolId = 1, // attempt to escalate
            Email = "still-low-priv@x.com",
            Name = "X",
            UserName = "x",
            Number = "0",
            IdentificationDocument = "0",
        };

        current.UserId = UserAId;
        var response = await svc.UpdateAsync(request);

        Assert.True(response.Success, $"Update failed: status={response.Status} message={response.Message} errors={string.Join(",", response.Errors?.Select(e => e.ErrorMessage) ?? Array.Empty<string>())}");
        var reloaded = await db.Users.AsNoTracking().FirstAsync(u => u.Id == userId);
        Assert.Equal(2, reloaded.RolId);
    }

    // ---------------------------------------------------------------------
    // B3 — Filter extraction oracle.
    // ---------------------------------------------------------------------

    [Fact]
    public async Task GetAll_WithFilterOnPassword_ReturnsValidationFailed()
    {
        var (svc, db, current) = await BuildSeededAsync();
        await SeedUserAsync(db, ownerId: UserAId);

        current.UserId = UserAId;
        var response = await svc.GetAllAsync(new QueryOptions
        {
            Filters = "Password:like:$2a$",
        });

        Assert.False(response.Success);
        Assert.Equal(ResponseStatus.ValidationFailed, response.Status);
    }

    // ---------------------------------------------------------------------
    // B8 — RBAC fail-closed.
    // ---------------------------------------------------------------------

    [Fact]
    public void RequireOperationConvention_RejectsControllerWithoutAttribute()
    {
        var provider = new Project.Server.Configs.Extensions.RequireOperationConventionProvider();

        var ctx = new Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModelProviderContext(
            new List<TypeInfo>());

        var controller = new Microsoft.AspNetCore.Mvc.ApplicationModels.ControllerModel(
            typeof(string).GetTypeInfo(),
            new List<object>());
        controller.ControllerName = "Test";
        controller.Actions.Add(new Microsoft.AspNetCore.Mvc.ApplicationModels.ActionModel(
            typeof(string).GetMethod(nameof(string.Trim), Type.EmptyTypes)!,
            new List<object>())
        {
            ActionName = "Trim",
        });

        ctx.Result.Controllers.Add(controller);

        Assert.Throws<InvalidOperationException>(() => provider.OnProvidersExecuted(ctx));
    }

    // ---------------------------------------------------------------------
    // B11 — Exceptions never leak through response.Message.
    // ---------------------------------------------------------------------

    [Fact]
    public async Task ServiceException_DoesNotLeakMessageToClient()
    {
        var (svc, db, current) = await BuildSeededAsync();
        await SeedUserAsync(db, ownerId: UserAId);

        current.UserId = UserAId;
        var response = await svc.GetAllAsync(new QueryOptions
        {
            Filters = "Name:unknownop:value",
        });

        Assert.False(response.Success);
        Assert.NotEqual(ResponseStatus.Ok, response.Status);
        // The client-facing message must NOT echo the translator's internal text.
        Assert.DoesNotContain("stack", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ---------------------------------------------------------------------
    // B13 — PageSize is clamped to MaxPageSize.
    // ---------------------------------------------------------------------

    [Fact]
    public async Task GetAll_PageSize10000_ClampsTo100()
    {
        var (svc, db, current) = await BuildSeededAsync();
        for (var i = 0; i < 150; i++)
        {
            await SeedUserAsync(db, ownerId: UserAId, name: $"u{i}");
        }

        current.UserId = UserAId;
        var response = await svc.GetAllAsync(new QueryOptions
        {
            PageSize = 10_000,
            PageNumber = 1,
            IncludeTotal = true,
        });

        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(100, response.Data.Count); // PaginationOptions.MaxPageSize
    }

    // ---------------------------------------------------------------------
    // B14 — Soft-delete via global query filter.
    // ---------------------------------------------------------------------

    [Fact]
    public async Task GetById_SoftDeleted_ReturnsNotFound()
    {
        var (svc, db, current) = await BuildSeededAsync();
        long id = await SeedUserAsync(db, ownerId: UserAId, state: 0);

        current.UserId = UserAId;
        var response = await svc.GetByIdAsync(id);

        Assert.False(response.Success);
        Assert.Equal(ResponseStatus.NotFound, response.Status);
    }

    // =====================================================================
    // Test harness
    // =====================================================================

    private sealed class FakeCurrentUser : ICurrentUserService
    {
        public long UserId { get; set; }
        public long? UserIdOrNull => UserId;
        public long RolId { get; set; }
        public bool IsAuthenticated => UserId != 0;
        public IReadOnlySet<string> OperationKeys { get; } = new HashSet<string>();
        public bool HasOperation(string operationKey) => OperationKeys.Contains(operationKey);
    }

    private sealed class NoOpSupportService : IEntitySupportService
    {
        public IValidator<TRequest> GetValidator<TRequest>(string key) => new InlineValidator<TRequest>();

        public IEnumerable<IEntityBeforeCreateInterceptor<TEntity, TRequest>> GetBeforeCreateInterceptors<TEntity, TRequest>() => Array.Empty<IEntityBeforeCreateInterceptor<TEntity, TRequest>>();
        public IEnumerable<IEntityAfterCreateInterceptor<TEntity, TRequest>> GetAfterCreateInterceptors<TEntity, TRequest>() => Array.Empty<IEntityAfterCreateInterceptor<TEntity, TRequest>>();
        public IEnumerable<IEntityBeforeUpdateInterceptor<TEntity, TRequest>> GetBeforeUpdateInterceptors<TEntity, TRequest>() => Array.Empty<IEntityBeforeUpdateInterceptor<TEntity, TRequest>>();
        public IEnumerable<IEntityAfterUpdateInterceptor<TEntity, TRequest>> GetAfterUpdateInterceptors<TEntity, TRequest>() => Array.Empty<IEntityAfterUpdateInterceptor<TEntity, TRequest>>();
        public IEnumerable<IEntityBeforePartialUpdateInterceptor<TEntity, TRequest>> GetBeforePartialUpdateInterceptors<TEntity, TRequest>() => Array.Empty<IEntityBeforePartialUpdateInterceptor<TEntity, TRequest>>();
        public IEnumerable<IEntityAfterPartialUpdateInterceptor<TEntity, TRequest>> GetAfterPartialUpdateInterceptors<TEntity, TRequest>() => Array.Empty<IEntityAfterPartialUpdateInterceptor<TEntity, TRequest>>();
        public IEnumerable<IEntityBeforeDeleteInterceptor<TEntity, TRequest>> GetBeforeDeleteInterceptors<TEntity, TRequest>() => Array.Empty<IEntityBeforeDeleteInterceptor<TEntity, TRequest>>();
        public IEnumerable<IEntityAfterDeleteInterceptor<TEntity, TRequest>> GetAfterDeleteInterceptors<TEntity, TRequest>() => Array.Empty<IEntityAfterDeleteInterceptor<TEntity, TRequest>>();
        public IEnumerable<IEntityQueryFilter<TEntity>> GetQueryFilters<TEntity>() where TEntity : class => Array.Empty<IEntityQueryFilter<TEntity>>();
    }

    private static async Task<(EntityService<User, UserRequest, long> svc, DataContext db, FakeCurrentUser current)> BuildSeededAsync()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString("N"))
            .Options;
        var db = new DataContext(options);
        await db.Database.EnsureCreatedAsync();

        var current = new FakeCurrentUser();
        var svc = new EntityService<User, UserRequest, long>(
            mapper: new Mapper(),
            logger: NullLogger<EntityService<User, UserRequest, long>>.Instance,
            db: db,
            filterTranslator: new FilterTranslator(),
            entitySupportService: new NoOpSupportService(),
            currentUser: current,
            queryPolicy: QueryPolicy<User>.Instance,
            sortTranslator: new SortTranslator(),
            pagination: new PaginationOptions());
        return (svc, db, current);
    }

    private static async Task<long> SeedUserAsync(
        DataContext db,
        long ownerId,
        int state = 1,
        long rolId = 2,
        string name = "u")
    {
        var u = new User
        {
            RolId = rolId,
            Password = "$2a$12$xxx",
            Email = $"{name}@x.com",
            Name = name,
            IdentificationDocument = name,
            UserName = name,
            Number = name,
            State = state,
            CreatedBy = ownerId,
            CreatedAt = DateTime.UtcNow,
        };
        db.Users.Add(u);
        await db.SaveChangesAsync();
        return u.Id;
    }
}