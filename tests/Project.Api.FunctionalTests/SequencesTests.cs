using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Project.Api.FunctionalTests.Infrastructure;
using Project.Application.Sequences;
using Project.Application.Tenancy;

namespace Project.Api.FunctionalTests;

/// <summary>Sequences module: unique numbers under concurrency, one counter per tenant, formats per tenant.</summary>
public sealed class SequencesTests(SharedModeApi api) : IClassFixture<SharedModeApi>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Concurrent_callers_get_unique_consecutive_numbers_per_tenant()
    {
        var first = await CreateTenantAsync();
        var second = await CreateTenantAsync();
        var year = DateTimeOffset.UtcNow.Year.ToString(CultureInfo.InvariantCulture);

        var numbers = new ConcurrentBag<string>();
        await Parallel.ForEachAsync(Enumerable.Range(0, 30), new ParallelOptions { MaxDegreeOfParallelism = 8, CancellationToken = Ct }, async (_, token) =>
            numbers.Add(await NextAsync(first.Id, token)));

        numbers.Distinct().Count().ShouldBe(30);
        numbers.Order().ShouldBe(Enumerable.Range(1, 30).Select(n => $"T-{year}-{n:0000}"));
        (await NextAsync(second.Id, Ct)).ShouldBe($"T-{year}-0001");

        // The tenant changes the format; the counter continues.
        var admin = await api.SignInAsync(host: first.Host);
        (await admin.PutAsJsonAsync("/api/v1/sequences/tests.invoice", new { format = "{0000000}" }, Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await NextAsync(first.Id, Ct)).ShouldBe("0000031");
        (await admin.PutAsJsonAsync("/api/v1/sequences/tests.invoice", new { format = "NO-COUNTER" }, Ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var listed = (await (await admin.GetAsync("/api/v1/sequences", Ct)).ReadArrayAsync()).Single(s => s!["name"]!.GetValue<string>() == "tests.invoice")!;
        listed["format"]!.GetValue<string>().ShouldBe("{0000000}");
    }

    private async Task<string> NextAsync(Guid tenantId, CancellationToken ct)
    {
        string? number = null;
        var found = await api.Services.GetRequiredService<ITenantJobRunner>().RunForTenantAsync(tenantId, async (services, _, token) =>
            number = await services.GetRequiredService<ISequenceGenerator>().NextAsync("tests.invoice", token), ct);
        found.ShouldBeTrue();
        return number!;
    }

    private async Task<(Guid Id, string Host)> CreateTenantAsync()
    {
        var code = $"seq{Guid.NewGuid():N}"[..12];
        var platform = await api.SignInAsync();
        var tenant = await (await platform.PostAsJsonAsync("/api/v1/tenancy/tenants",
            new { code, name = code, isolation = "Shared", primaryDomain = $"{code}.test" }, Ct)).ReadJsonAsync();
        (await platform.PostAsync($"/api/v1/tenancy/tenants/{tenant.Id()}/provision", null, Ct)).EnsureSuccessStatusCode();
        return (tenant.Id(), $"{code}.test");
    }
}
