using BillFlow.Api.Services;
using BillFlow.Api.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace BillFlow.Api.Tests.Workers;

public class DunningBackgroundWorkerTests
{
    private static IConfiguration FastIntervalConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Dunning:SweepIntervalMinutes"] = "0.0001" })
            .Build();

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20);
        }
    }

    [Fact]
    public async Task ExecuteAsync_TicksOnConfiguredInterval_AndSweepsAtLeastOnce()
    {
        var sweeper = new CountingSweeper();
        await using var provider = new ServiceCollection().AddSingleton<IDunningSweeper>(sweeper).BuildServiceProvider();

        var worker = new DunningBackgroundWorker(
            provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<DunningBackgroundWorker>.Instance, FastIntervalConfig());

        await worker.StartAsync(CancellationToken.None);
        await WaitUntilAsync(() => sweeper.CallCount >= 1, TimeSpan.FromSeconds(2));
        await worker.StopAsync(CancellationToken.None);

        Assert.True(sweeper.CallCount >= 1);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSweepThrows_KeepsRunningInsteadOfCrashing()
    {
        var sweeper = new ThrowingSweeper();
        await using var provider = new ServiceCollection().AddSingleton<IDunningSweeper>(sweeper).BuildServiceProvider();

        var worker = new DunningBackgroundWorker(
            provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<DunningBackgroundWorker>.Instance, FastIntervalConfig());

        await worker.StartAsync(CancellationToken.None);
        await WaitUntilAsync(() => sweeper.CallCount >= 2, TimeSpan.FromSeconds(2));
        await worker.StopAsync(CancellationToken.None);

        Assert.True(sweeper.CallCount >= 2);
    }

    private class CountingSweeper : IDunningSweeper
    {
        private int _callCount;
        public int CallCount => _callCount;

        public Task<DunningSweepResult> RunAsync(CancellationToken ct = default)
        {
            Interlocked.Increment(ref _callCount);
            return Task.FromResult(new DunningSweepResult(0, 0));
        }
    }

    private class ThrowingSweeper : IDunningSweeper
    {
        private int _callCount;
        public int CallCount => _callCount;

        public Task<DunningSweepResult> RunAsync(CancellationToken ct = default)
        {
            Interlocked.Increment(ref _callCount);
            throw new InvalidOperationException("Simulated sweep failure.");
        }
    }
}
