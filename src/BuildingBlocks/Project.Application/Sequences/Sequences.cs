using Microsoft.Extensions.DependencyInjection;

namespace Project.Application.Sequences;

public enum SequenceReset
{
    Never,
    Yearly,
    Monthly,
    Daily,
}

/// <summary>
/// A document numbering (invoices, orders, employees, requests). <see cref="Format"/> mixes text and tokens:
/// <c>{yyyy}</c>, <c>{yy}</c>, <c>{MM}</c>, <c>{dd}</c> and the counter padded with zeros (<c>{0000}</c>).
/// With <see cref="PerOrganizationUnit"/> each unit (branch, brand) has its own counter.
/// </summary>
public sealed record SequenceDefinition(string Name, string Format, SequenceReset Reset = SequenceReset.Never, bool PerOrganizationUnit = false, string? Description = null);

/// <summary>
/// Next number of a sequence, unique and increasing per tenant (and unit), safe with concurrent requests and several
/// instances. A number is consumed even if the caller's transaction later fails, so reserve it right before saving.
/// </summary>
public interface ISequenceGenerator
{
    Task<string> NextAsync(string sequence, CancellationToken ct);
}

public static class SequenceExtensions
{
    /// <summary>
    /// Declares a sequence: <c>services.AddSequence(new("sales.invoice", "FAC-{yyyy}-{00000}", SequenceReset.Yearly))</c>.
    /// Tenants may change the format without code.
    /// </summary>
    public static IServiceCollection AddSequence(this IServiceCollection services, SequenceDefinition definition) =>
        services.AddSingleton(definition);
}
