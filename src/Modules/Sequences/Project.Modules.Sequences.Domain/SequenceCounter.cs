using Project.SharedKernel.Domain;

namespace Project.Modules.Sequences.Domain;

/// <summary>Last number issued by a sequence for one tenant, unit (<see cref="ScopeKey"/>) and period.</summary>
public sealed class SequenceCounter : AggregateRoot<Guid>, IMultiTenant
{
    private SequenceCounter()
    {
    }

    public Guid TenantId { get; set; }

    public string Name { get; private set; } = string.Empty;

    /// <summary>Organization unit for per-unit sequences; empty otherwise.</summary>
    public string ScopeKey { get; private set; } = string.Empty;

    /// <summary><c>2026</c>, <c>202610</c>, <c>20261013</c> or empty, according to the reset policy.</summary>
    public string Period { get; private set; } = string.Empty;

    public long Value { get; private set; }

    public static SequenceCounter Start(string name, string scopeKey, string period) =>
        new() { Id = NewId.Next(), Name = name, ScopeKey = scopeKey, Period = period, Value = 1 };
}

/// <summary>Format of a sequence chosen by the tenant (replaces the default of the module).</summary>
public sealed class SequenceFormat : AuditableAggregateRoot, IMultiTenant
{
    public Guid TenantId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Format { get; set; } = string.Empty;
}
