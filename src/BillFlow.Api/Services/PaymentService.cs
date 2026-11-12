using BillFlow.Api.Data;
using BillFlow.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.Api.Services;

public class PaymentService : IPaymentService
{
    private readonly BillFlowDbContext _db;
    private readonly IPaymentGateway _gateway;
    private readonly IClock _clock;

    public PaymentService(BillFlowDbContext db, IPaymentGateway gateway, IClock clock)
    {
        _db = db;
        _gateway = gateway;
        _clock = clock;
    }

    public async Task<PaymentResult> ChargeInvoiceAsync(int invoiceId, string idempotencyKey, CancellationToken ct = default)
    {
        var existingPayment = await _db.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey, ct);
        if (existingPayment is not null)
        {
            return await BuildResultFromExistingPaymentAsync(existingPayment, ct);
        }

        var invoice = await _db.Invoices
            .Include(i => i.Subscription)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, ct)
            ?? throw new EntityNotFoundException(nameof(Invoice), invoiceId);

        if (invoice.Status == InvoiceStatus.Paid)
        {
            return new PaymentResult(true, invoice.Status, null);
        }

        if (invoice.Status is InvoiceStatus.Void or InvoiceStatus.Uncollectible)
        {
            throw new InvalidStateTransitionException($"Invoice {invoice.Number} is {invoice.Status} and can no longer be charged.");
        }

        var customer = await _db.Customers.FindAsync(new object[] { invoice.Subscription.CustomerId }, ct)
            ?? throw new EntityNotFoundException(nameof(Customer), invoice.Subscription.CustomerId);

        var gatewayResult = await _gateway.ChargeAsync(invoice.Amount, customer.Email, ct);
        var now = _clock.UtcNow;

        invoice.AttemptCount++;
        _db.Payments.Add(new Payment
        {
            InvoiceId = invoice.Id,
            IdempotencyKey = idempotencyKey,
            Amount = invoice.Amount,
            Status = gatewayResult.Succeeded ? PaymentStatus.Succeeded : PaymentStatus.Failed,
            FailureReason = gatewayResult.FailureReason,
            CreatedAt = now,
        });

        if (gatewayResult.Succeeded)
        {
            ApplySuccessfulPayment(invoice, now);
        }
        else
        {
            ApplyFailedPayment(invoice, now);
        }

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Another request recorded a payment under the same idempotency key first (unique index race).
            _db.ChangeTracker.Clear();
            var racedPayment = await _db.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey, ct);
            if (racedPayment is null)
            {
                throw;
            }

            return await BuildResultFromExistingPaymentAsync(racedPayment, ct);
        }

        return new PaymentResult(gatewayResult.Succeeded, invoice.Status, gatewayResult.FailureReason);
    }

    private static void ApplySuccessfulPayment(Invoice invoice, DateTime now)
    {
        invoice.Status = InvoiceStatus.Paid;
        invoice.PaidAt = now;
        invoice.NextRetryAt = null;

        if (invoice.Subscription.Status == SubscriptionStatus.PastDue)
        {
            invoice.Subscription.Status = SubscriptionStatus.Active;
        }
    }

    private static void ApplyFailedPayment(Invoice invoice, DateTime now)
    {
        if (DunningPolicy.ShouldWriteOff(invoice.AttemptCount))
        {
            invoice.Status = InvoiceStatus.Uncollectible;
            invoice.NextRetryAt = null;

            if (SubscriptionTransitions.CanTransition(invoice.Subscription.Status, SubscriptionStatus.Canceled))
            {
                invoice.Subscription.Status = SubscriptionStatus.Canceled;
                invoice.Subscription.CanceledAt = now;
            }

            return;
        }

        invoice.NextRetryAt = DunningPolicy.NextRetryAt(invoice.AttemptCount, now);

        if (SubscriptionTransitions.CanTransition(invoice.Subscription.Status, SubscriptionStatus.PastDue))
        {
            invoice.Subscription.Status = SubscriptionStatus.PastDue;
        }
    }

    private async Task<PaymentResult> BuildResultFromExistingPaymentAsync(Payment payment, CancellationToken ct)
    {
        var invoice = await _db.Invoices.AsNoTracking().FirstAsync(i => i.Id == payment.InvoiceId, ct);
        return new PaymentResult(payment.Status == PaymentStatus.Succeeded, invoice.Status, payment.FailureReason);
    }
}
