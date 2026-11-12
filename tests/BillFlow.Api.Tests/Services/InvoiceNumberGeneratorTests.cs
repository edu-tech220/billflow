using BillFlow.Api.Services;
using BillFlow.Api.Tests.TestSupport;

namespace BillFlow.Api.Tests.Services;

public class InvoiceNumberGeneratorTests
{
    [Fact]
    public async Task SequentialCalls_ProduceIncreasingFormattedNumbers()
    {
        using var factory = new SqliteDbContextFactory();

        await using var context = factory.CreateContext();
        var generator = new InvoiceNumberGenerator(context);

        var first = await generator.NextAsync(2026);
        var second = await generator.NextAsync(2026);
        var third = await generator.NextAsync(2026);

        Assert.Equal("INV-2026-000001", first);
        Assert.Equal("INV-2026-000002", second);
        Assert.Equal("INV-2026-000003", third);
    }

    [Fact]
    public async Task DifferentYears_AreCountedIndependently()
    {
        using var factory = new SqliteDbContextFactory();

        await using var context = factory.CreateContext();
        var generator = new InvoiceNumberGenerator(context);

        var firstYearOne = await generator.NextAsync(2026);
        var firstYearTwo = await generator.NextAsync(2027);
        var secondYearOne = await generator.NextAsync(2026);

        Assert.Equal("INV-2026-000001", firstYearOne);
        Assert.Equal("INV-2027-000001", firstYearTwo);
        Assert.Equal("INV-2026-000002", secondYearOne);
    }
}
