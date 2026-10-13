using Microsoft.Extensions.Options;

namespace Project.UnitTests;

/// <summary>Fixed <see cref="IOptionsMonitor{TOptions}"/> for unit tests.</summary>
internal sealed class TestOptionsMonitor<T>(T value) : IOptionsMonitor<T>
{
    public T CurrentValue { get; set; } = value;

    public T Get(string? name) => CurrentValue;

    public IDisposable? OnChange(Action<T, string?> listener) => null;
}
