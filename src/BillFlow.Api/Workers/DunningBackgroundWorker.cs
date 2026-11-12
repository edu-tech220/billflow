using BillFlow.Api.Services;

namespace BillFlow.Api.Workers;

/// <summary>
/// Runs the dunning sweep on a timer: renews subscriptions whose billing period just ended
/// and retries open invoices whose backoff delay has elapsed. Each tick resolves its own DI
/// scope since IDunningSweeper depends on a scoped DbContext.
/// </summary>
public class DunningBackgroundWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DunningBackgroundWorker> _logger;
    private readonly TimeSpan _interval;

    public DunningBackgroundWorker(IServiceScopeFactory scopeFactory, ILogger<DunningBackgroundWorker> logger, IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _interval = TimeSpan.FromMinutes(config.GetValue("Dunning:SweepIntervalMinutes", 5));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);

        do
        {
            await RunSweepAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunSweepAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var sweeper = scope.ServiceProvider.GetRequiredService<IDunningSweeper>();
            var result = await sweeper.RunAsync(ct);

            if (result.RenewalsProcessed > 0 || result.PaymentRetriesProcessed > 0)
            {
                _logger.LogInformation(
                    "Dunning sweep processed {Renewals} renewal(s) and {Retries} payment retry(ies).",
                    result.RenewalsProcessed, result.PaymentRetriesProcessed);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Dunning sweep failed.");
        }
    }
}
