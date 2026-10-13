using System.Globalization;
using System.Text.RegularExpressions;
using Project.Application.Abstractions;
using Project.Application.Messaging;
using Project.Application.Security;
using Project.Application.Sequences;
using Project.Modules.Sequences.Contracts;
using Project.Modules.Sequences.Domain;
using Project.SharedKernel;

namespace Project.Modules.Sequences.Application;

/// <summary>Renders <c>FAC-{yyyy}-{0000}</c> style formats.</summary>
public static partial class SequenceFormatter
{
    public static bool IsValid(string format) => format.Length is > 0 and <= 64 && Counter().IsMatch(format);

    public static string Format(string format, long value, DateTimeOffset date) =>
        Token().Replace(format, match => match.Groups[1].Value switch
        {
            "yyyy" => date.ToString("yyyy", CultureInfo.InvariantCulture),
            "yy" => date.ToString("yy", CultureInfo.InvariantCulture),
            "MM" => date.ToString("MM", CultureInfo.InvariantCulture),
            "dd" => date.ToString("dd", CultureInfo.InvariantCulture),
            var zeros when zeros.All(c => c == '0') => value.ToString(new string('0', zeros.Length), CultureInfo.InvariantCulture),
            _ => match.Value,
        });

    public static string Period(SequenceReset reset, DateTimeOffset date) => reset switch
    {
        SequenceReset.Yearly => date.ToString("yyyy", CultureInfo.InvariantCulture),
        SequenceReset.Monthly => date.ToString("yyyyMM", CultureInfo.InvariantCulture),
        SequenceReset.Daily => date.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
        _ => string.Empty,
    };

    [GeneratedRegex(@"\{(yyyy|yy|MM|dd|0+)\}")]
    private static partial Regex Token();

    [GeneratedRegex(@"\{0+\}")]
    private static partial Regex Counter();
}

public sealed record SequenceDto(string Name, string? Description, string DefaultFormat, string Format, string Reset, bool PerOrganizationUnit, string Example);

public sealed record ListSequencesQuery : IQuery<IReadOnlyList<SequenceDto>>, IRequirePermission
{
    public string Permission => SequencesPermissions.Sequences.Manage;
}

public sealed class ListSequencesHandler(IEnumerable<SequenceDefinition> definitions, IRepository<SequenceFormat, Guid> formats, TimeProvider clock)
    : IQueryHandler<ListSequencesQuery, IReadOnlyList<SequenceDto>>
{
    public async ValueTask<Result<IReadOnlyList<SequenceDto>>> Handle(ListSequencesQuery query, CancellationToken cancellationToken)
    {
        var custom = await formats.ListAsync(new Specification<SequenceFormat>(), cancellationToken);
        var now = clock.GetUtcNow();
        IReadOnlyList<SequenceDto> items = definitions
            .OrderBy(d => d.Name, StringComparer.Ordinal)
            .Select(d =>
            {
                var format = custom.FirstOrDefault(c => c.Name == d.Name)?.Format ?? d.Format;
                return new SequenceDto(d.Name, d.Description, d.Format, format, d.Reset.ToString(), d.PerOrganizationUnit, SequenceFormatter.Format(format, 1, now));
            })
            .ToList();
        return Result<IReadOnlyList<SequenceDto>>.Success(items);
    }
}

/// <summary>Changes the format of a sequence for the tenant; an empty format restores the default.</summary>
public sealed record UpdateSequenceFormatCommand(string Name, string? Format) : ICommand, IRequirePermission
{
    public string Permission => SequencesPermissions.Sequences.Manage;
}

public sealed class UpdateSequenceFormatHandler(IEnumerable<SequenceDefinition> definitions, IRepository<SequenceFormat, Guid> formats)
    : ICommandHandler<UpdateSequenceFormatCommand>
{
    public async ValueTask<Result> Handle(UpdateSequenceFormatCommand command, CancellationToken cancellationToken)
    {
        if (definitions.All(d => d.Name != command.Name))
        {
            return Error.NotFound("sequences.not_found");
        }

        var reset = string.IsNullOrWhiteSpace(command.Format);
        if (!reset && !SequenceFormatter.IsValid(command.Format!.Trim()))
        {
            return Error.Validation("format", "The format needs a counter such as {0000} and at most 64 characters.");
        }

        var current = await formats.FirstOrDefaultAsync(new Specification<SequenceFormat>().Where(f => f.Name == command.Name).Tracked(), cancellationToken);
        if (reset)
        {
            if (current is not null)
            {
                formats.Remove(current);
            }
        }
        else if (current is null)
        {
            formats.Add(new SequenceFormat { Name = command.Name, Format = command.Format!.Trim() });
        }
        else
        {
            current.Format = command.Format!.Trim();
        }

        await formats.UnitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class SequencesPermissionProvider : IPermissionProvider
{
    public IEnumerable<PermissionDefinition> GetPermissions() =>
    [
        new(SequencesPermissions.Sequences.Manage, SequencesPermissions.Module, "permissions.sequences.sequences.manage", "sequences"),
    ];

    public IEnumerable<NavigationDefinition> GetNavigation() =>
    [
        new("sequences.sequences", SequencesPermissions.Module, "nav.sequences", "/sequences", "hash", SequencesPermissions.Sequences.Manage, 830),
    ];
}
