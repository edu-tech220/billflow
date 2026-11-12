using BillFlow.Api.Data;
using BillFlow.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.Api.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly BillFlowDbContext _db;
    private readonly IBillingService _billing;
    private readonly IPaymentService _payments;
    private readonly IClock _clock;

    public SubscriptionService(BillFlowDbContext db, IBillingService billing, IPaymentService payments, IClock clock)
    {
        _db = db;
        _billing = billing;
        _payments = payments;
        _clock = clock;
    }

    public async Task<Subscription> CreateAsync(int customerId, int planId, CancellationToken ct = default)
    {
        var customer = await _db.Customers.FindAsync(new object[] { customerId }, ct)
            ?? throw new EntityNotFoundException(nameof(Customer), customerId);
        var plan = await _db.Plans.FindAsync(new object[] { planId }, ct)
            ?? throw new EntityNotFoundException(nameof(Plan), planId);

        if (!plan.IsActive)
        {
            throw new InvalidStateTransitionException($"Plan '{plan.Code}' is no longer available for new subscriptions.");
        }

        var hasOpenSubscription = await _db.Subscriptions.AnyAsync(
            s => s.CustomerId == customerId
                && (s.Status == SubscriptionStatus.Trialing || s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.PastDue),
            ct);
        if (hasOpenSubscription)
        {
            throw new InvalidStateTransitionException("Customer already has an active subscription.");
        }

        var now = _clock.UtcNow;
        var isTrial = plan.TrialDays > 0;

        var subscription = new Subscription
        {
            CustomerId = customerId,
            PlanId = planId,
            Status = isTrial ? SubscriptionStatus.Trialing : SubscriptionStatus.Active,
            CurrentPeriodStart = now,
            CurrentPeriodEnd = isTrial ? now.AddDays(plan.TrialDays) : now.AddMonths(1),
            TrialEndsAt = isTrial ? now.AddDays(plan.TrialDays) : null,
        };

        _db.Subscriptions.Add(subscription);
        await _db.SaveChangesAsync(ct);

        if (!isTrial)
        {
            var invoice = await _billing.CreateRenewalInvoiceAsync(subscription, subscription.CurrentPeriodStart, subscription.CurrentPeriodEnd, ct);
            if (invoice.Status == InvoiceStatus.Open)
            {
                await _payments.ChargeInvoiceAsync(invoice.Id, $"initial-{subscription.Id}", ct);
            }
        }

        return subscription;
    }

    public async Task<Subscription> ChangePlanAsync(int subscriptionId, int newPlanId, CancellationToken ct = default)
    {
        var subscription = await _db.Subscriptions.FirstOrDefaultAsync(s => s.Id == subscriptionId, ct)
            ?? throw new EntityNotFoundException(nameof(Subscription), subscriptionId);

        if (subscription.Status != SubscriptionStatus.Active)
        {
            throw new InvalidStateTransitionException("Only an active subscription can change plans.");
        }

        var oldPlan = await _db.Plans.FindAsync(new object[] { subscription.PlanId }, ct)
            ?? throw new EntityNotFoundException(nameof(Plan), subscription.PlanId);
        var newPlan = await _db.Plans.FindAsync(new object[] { newPlanId }, ct)
            ?? throw new EntityNotFoundException(nameof(Plan), newPlanId);

        if (!newPlan.IsActive)
        {
            throw new InvalidStateTransitionException($"Plan '{newPlan.Code}' is no longer available.");
        }

        if (newPlan.Id == oldPlan.Id)
        {
            return subscription;
        }

        var now = _clock.UtcNow;
        var invoice = await _billing.ApplyPlanChangeAsync(subscription, oldPlan, newPlan, now, ct);

        subscription.PlanId = newPlan.Id;
        await _db.SaveChangesAsync(ct);

        if (invoice is { Status: InvoiceStatus.Open })
        {
            await _payments.ChargeInvoiceAsync(invoice.Id, $"planchange-{invoice.Id}", ct);
        }

        return subscription;
    }

    public async Task<Subscription> CancelAsync(int subscriptionId, bool atPeriodEnd, CancellationToken ct = default)
    {
        var subscription = await _db.Subscriptions.FirstOrDefaultAsync(s => s.Id == subscriptionId, ct)
            ?? throw new EntityNotFoundException(nameof(Subscription), subscriptionId);

        if (atPeriodEnd)
        {
            if (subscription.Status is SubscriptionStatus.Canceled or SubscriptionStatus.Expired)
            {
                throw new InvalidStateTransitionException($"Subscription is already {subscription.Status}.");
            }

            subscription.CancelAtPeriodEnd = true;
        }
        else
        {
            SubscriptionTransitions.EnsureTransition(subscription.Status, SubscriptionStatus.Canceled);
            subscription.Status = SubscriptionStatus.Canceled;
            subscription.CanceledAt = _clock.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return subscription;
    }

    public async Task<Subscription> RenewAsync(int subscriptionId, CancellationToken ct = default)
    {
        var subscription = await _db.Subscriptions.FirstOrDefaultAsync(s => s.Id == subscriptionId, ct)
            ?? throw new EntityNotFoundException(nameof(Subscription), subscriptionId);

        if (subscription.Status is not (SubscriptionStatus.Trialing or SubscriptionStatus.Active))
        {
            throw new InvalidStateTransitionException($"Subscription in status {subscription.Status} is not eligible for renewal.");
        }

        var now = _clock.UtcNow;

        if (subscription.CancelAtPeriodEnd)
        {
            SubscriptionTransitions.EnsureTransition(subscription.Status, SubscriptionStatus.Canceled);
            subscription.Status = SubscriptionStatus.Canceled;
            subscription.CanceledAt = now;
            await _db.SaveChangesAsync(ct);
            return subscription;
        }

        if (subscription.Status == SubscriptionStatus.Trialing)
        {
            SubscriptionTransitions.EnsureTransition(SubscriptionStatus.Trialing, SubscriptionStatus.Active);
            subscription.Status = SubscriptionStatus.Active;
        }

        var newPeriodStart = subscription.CurrentPeriodEnd;
        var newPeriodEnd = newPeriodStart.AddMonths(1);

        var invoice = await _billing.CreateRenewalInvoiceAsync(subscription, newPeriodStart, newPeriodEnd, ct);

        subscription.CurrentPeriodStart = newPeriodStart;
        subscription.CurrentPeriodEnd = newPeriodEnd;
        await _db.SaveChangesAsync(ct);

        if (invoice.Status == InvoiceStatus.Open)
        {
            await _payments.ChargeInvoiceAsync(invoice.Id, $"renewal-{invoice.Id}", ct);
        }

        return subscription;
    }
}
