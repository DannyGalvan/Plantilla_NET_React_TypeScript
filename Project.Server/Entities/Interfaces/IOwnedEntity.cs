namespace Project.Server.Entities.Interfaces
{
    /// <summary>
    /// Marks an entity as owned by a user. The owner is resolved from the JWT at runtime
    /// via <c>OwnershipResolver</c> and used to scope every CRUD read/write through the
    /// generic <c>IEntityService</c>.
    /// </summary>
    /// <remarks>
    /// If an entity is not <c>IEntity&lt;T&gt;</c> AND not <c>IOwnedEntity&lt;TOwnerId&gt;</c>
    /// then <c>OwnershipValidationHostedService</c> refuses to start the application:
    /// exposing a non-owned entity is a footgun.
    /// </remarks>
    public interface IOwnedEntity<TOwnerId> where TOwnerId : struct
    {
        TOwnerId OwnerId { get; set; }
    }
}