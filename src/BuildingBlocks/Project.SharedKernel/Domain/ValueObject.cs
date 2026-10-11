namespace Project.SharedKernel.Domain;

/// <summary>
/// Base value object with structural equality.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    public bool Equals(ValueObject? other) =>
        other is not null
        && other.GetType() == GetType()
        && GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());

    public override bool Equals(object? obj) => Equals(obj as ValueObject);

    public override int GetHashCode() =>
        GetEqualityComponents().Aggregate(17, (hash, component) => HashCode.Combine(hash, component));

    protected abstract IEnumerable<object?> GetEqualityComponents();
}

/// <summary>Sequential identifiers (UUID v7): portable, generated in the app and index friendly.</summary>
public static class NewId
{
    public static Guid Next() => Guid.CreateVersion7();
}
