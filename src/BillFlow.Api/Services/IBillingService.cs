using BillFlow.Api.Domain;

namespace BillFlow.Api.Services;

public interface IBillingService
{
    /// <summary>Issues the invoice for a subscription's next billing period, applying any available credit.</summary>
    Task<Invoice> CreateRenewalInvoiceAsync(Subscription subscription, DateTime periodStart, DateTime periodEnd, CancellationToken ct = default);

    /// <summary>
    /// Settles a mid-cycle plan change. A net charge produces an immediately-due proration
    /// invoice; a net credit is added to the customer's balance and no invoice is created.
    /// </summary>
    Task<Invoice?> ApplyPlanChangeAsync(Subscription subscription, Plan oldPlan, Plan newPlan, DateTime changeDate, CancellationToken ct = default);
}
