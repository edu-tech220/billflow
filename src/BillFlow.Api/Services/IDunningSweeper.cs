namespace BillFlow.Api.Services;

public record DunningSweepResult(int RenewalsProcessed, int PaymentRetriesProcessed);

/// <summary>
/// One pass over everything billing-wise due right now: subscriptions whose period ended
/// get renewed (or canceled, if flagged), and open invoices whose retry date has arrived
/// get another collection attempt. Driven by an admin action or a recurring background job.
/// </summary>
public interface IDunningSweeper
{
    Task<DunningSweepResult> RunAsync(CancellationToken ct = default);
}
