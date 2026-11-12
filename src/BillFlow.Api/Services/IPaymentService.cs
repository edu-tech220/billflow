using BillFlow.Api.Domain;

namespace BillFlow.Api.Services;

public record PaymentResult(bool Succeeded, InvoiceStatus InvoiceStatus, string? FailureReason);

public interface IPaymentService
{
    /// <summary>
    /// Attempts to charge an invoice. Replaying the same <paramref name="idempotencyKey"/>
    /// (e.g. after a client retry or a dunning re-run) never charges twice — the stored
    /// result from the first attempt is returned instead.
    /// </summary>
    Task<PaymentResult> ChargeInvoiceAsync(int invoiceId, string idempotencyKey, CancellationToken ct = default);
}
