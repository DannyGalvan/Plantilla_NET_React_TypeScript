using System.Globalization;

namespace Project.SharedKernel.Domain;

/// <summary>
/// An amount in a currency (ISO 4217 code). Arithmetic between different currencies throws, so a quetzal is never
/// added to a dollar by mistake. Map it with <c>builder.ComplexProperty(x =&gt; x.Total)</c> (two columns).
/// </summary>
public readonly record struct Money
{
    public Money(decimal amount, string currency)
    {
        if (currency is not { Length: 3 } || !currency.All(char.IsAsciiLetterUpper))
        {
            throw new ArgumentException("The currency must be an ISO 4217 code in upper case, for example USD or GTQ.", nameof(currency));
        }

        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public string Currency { get; }

    public static Money Zero(string currency) => new(0, currency);

    public bool IsZero => Amount == 0;

    public bool IsNegative => Amount < 0;

    /// <summary>Rounds half away from zero (commercial rounding) to the given decimals.</summary>
    public Money Round(int decimals = 2) => new(Math.Round(Amount, decimals, MidpointRounding.AwayFromZero), Currency);

    /// <summary>
    /// Splits the amount in <paramref name="parts"/> that add up exactly to the original (the remainder goes to the
    /// first parts), for installments or prorations.
    /// </summary>
    public IReadOnlyList<Money> Allocate(int parts, int decimals = 2)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(parts);
        var unit = (decimal)Math.Pow(10, -decimals);
        var share = Math.Floor(Amount / parts / unit) * unit;
        var remainder = Amount - (share * parts);
        var result = new Money[parts];
        for (var i = 0; i < parts; i++)
        {
            var extra = remainder >= unit ? unit : 0;
            remainder -= extra;
            result[i] = new Money(share + extra, Currency);
        }

        return result;
    }

    public static Money operator +(Money left, Money right) => new(left.Amount + Same(left, right).Amount, left.Currency);

    public static Money operator -(Money left, Money right) => new(left.Amount - Same(left, right).Amount, left.Currency);

    public static Money operator -(Money value) => new(-value.Amount, value.Currency);

    public static Money operator *(Money value, decimal factor) => new(value.Amount * factor, value.Currency);

    public static Money operator /(Money value, decimal divisor) => new(value.Amount / divisor, value.Currency);

    public static bool operator >(Money left, Money right) => left.Amount > Same(left, right).Amount;

    public static bool operator <(Money left, Money right) => left.Amount < Same(left, right).Amount;

    public static bool operator >=(Money left, Money right) => left.Amount >= Same(left, right).Amount;

    public static bool operator <=(Money left, Money right) => left.Amount <= Same(left, right).Amount;

    public override string ToString() => $"{Amount.ToString("0.00##", CultureInfo.InvariantCulture)} {Currency}";

    private static Money Same(Money left, Money right) =>
        left.Currency == right.Currency
            ? right
            : throw new InvalidOperationException($"Cannot combine {left.Currency} and {right.Currency}; convert one of them first.");
}
