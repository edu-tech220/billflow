using BillFlow.Api.Data;
using BillFlow.Api.Domain;

namespace BillFlow.Api.Services;

public class BillingService : IBillingService
{
    private readonly BillFlowDbContext _db;
    private readonly IInvoiceNumberGenerator _invoiceNumbers;
    private readonly IClock _clock;

    public BillingService(BillFlowDbContext db, IInvoiceNumberGenerator invoiceNumbers, IClock clock)
    {
        _db = db;
        _invoiceNumbers = invoiceNumbers;
        _clock = clock;
    }

    public async Task<Invoice> CreateRenewalInvoiceAsync(Subscription subscription, DateTime periodStart, DateTime periodEnd, CancellationToken ct = default)
    {
        var customer = await _db.Customers.FindAsync(new object[] { subscription.CustomerId }, ct)
            ?? throw new EntityNotFoundException(nameof(Customer), subscription.CustomerId);
        var plan = await _db.Plans.FindAsync(new object[] { subscription.PlanId }, ct)
            ?? throw new EntityNotFoundException(nameof(Plan), subscription.PlanId);

        var amountDue = plan.MonthlyPrice;
        var creditApplied = Math.Min(customer.CreditBalance, amountDue);
        amountDue -= creditApplied;
        customer.CreditBalance -= creditApplied;

        var now = _clock.UtcNow;
        var invoice = new Invoice
        {
            SubscriptionId = subscription.Id,
            Number = await _invoiceNumbers.NextAsync(periodStart.Year, ct),
            Type = InvoiceType.Renewal,
            Amount = amountDue,
            Status = amountDue <= 0 ? InvoiceStatus.Paid : InvoiceStatus.Open,
            IssuedAt = now,
            DueAt = now,
            PaidAt = amountDue <= 0 ? now : null,
        };

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync(ct);
        return invoice;
    }

    public async Task<Invoice?> ApplyPlanChangeAsync(Subscription subscription, Plan oldPlan, Plan newPlan, DateTime changeDate, CancellationToken ct = default)
    {
        var customer = await _db.Customers.FindAsync(new object[] { subscription.CustomerId }, ct)
            ?? throw new EntityNotFoundException(nameof(Customer), subscription.CustomerId);

        var netCharge = ProrationCalculator.CalculateNetCharge(
            oldPlan.MonthlyPrice, newPlan.MonthlyPrice, subscription.CurrentPeriodStart, subscription.CurrentPeriodEnd, changeDate);

        if (netCharge <= 0)
        {
            customer.CreditBalance += Math.Abs(netCharge);
            await _db.SaveChangesAsync(ct);
            return null;
        }

        var now = _clock.UtcNow;
        var invoice = new Invoice
        {
            SubscriptionId = subscription.Id,
            Number = await _invoiceNumbers.NextAsync(changeDate.Year, ct),
            Type = InvoiceType.Proration,
            Amount = netCharge,
            Status = InvoiceStatus.Open,
            IssuedAt = now,
            DueAt = now,
        };

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync(ct);
        return invoice;
    }
}
