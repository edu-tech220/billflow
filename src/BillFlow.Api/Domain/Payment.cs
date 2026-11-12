namespace BillFlow.Api.Domain;

public class Payment
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    /// <summary>
    /// Client- or worker-supplied key that makes charging an invoice idempotent:
    /// replaying the same key never charges the customer twice.
    /// </summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
}
