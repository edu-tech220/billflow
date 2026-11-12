using BillFlow.Api.Data;
using BillFlow.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.Api.Services;

public class DunningSweepService : IDunningSweeper
{
    private readonly BillFlowDbContext _db;
    private readonly ISubscriptionService _subscriptions;
    private readonly IPaymentService _payments;
    private readonly IClock _clock;

    public DunningSweepService(BillFlowDbContext db, ISubscriptionService subscriptions, IPaymentService payments, IClock clock)
    {
        _db = db;
        _subscriptions = subscriptions;
        _payments = payments;
        _clock = clock;
    }

    public async Task<DunningSweepResult> RunAsync(CancellationToken ct = default)
    {
        var now = _clock.UtcNow;

        var dueSubscriptionIds = await _db.Subscriptions
            .Where(s => (s.Status == SubscriptionStatus.Trialing || s.Status == SubscriptionStatus.Active) && s.CurrentPeriodEnd <= now)
            .Select(s => s.Id)
            .ToListAsync(ct);

        foreach (var subscriptionId in dueSubscriptionIds)
        {
            await _subscriptions.RenewAsync(subscriptionId, ct);
        }

        var dueInvoices = await _db.Invoices
            .Where(i => i.Status == InvoiceStatus.Open && i.NextRetryAt != null && i.NextRetryAt <= now)
            .Select(i => new { i.Id, i.AttemptCount })
            .ToListAsync(ct);

        foreach (var invoice in dueInvoices)
        {
            var idempotencyKey = $"dunning-{invoice.Id}-{invoice.AttemptCount + 1}";
            await _payments.ChargeInvoiceAsync(invoice.Id, idempotencyKey, ct);
        }

        return new DunningSweepResult(dueSubscriptionIds.Count, dueInvoices.Count);
    }
}
