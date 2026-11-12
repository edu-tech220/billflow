namespace BillFlow.Api.Domain;

public class Invoice : IHasConcurrencyStamp
{
    public int Id { get; set; }

    public int SubscriptionId { get; set; }
    public Subscription Subscription { get; set; } = null!;

    /// <summary>Human-readable sequential number, e.g. "INV-2026-000001".</summary>
    public string Number { get; set; } = string.Empty;

    public InvoiceType Type { get; set; }
    public decimal Amount { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Open;

    public DateTime IssuedAt { get; set; }
    public DateTime DueAt { get; set; }
    public DateTime? PaidAt { get; set; }

    /// <summary>Number of payment attempts made against this invoice (initial + dunning retries).</summary>
    public int AttemptCount { get; set; }
    public DateTime? NextRetryAt { get; set; }

    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
