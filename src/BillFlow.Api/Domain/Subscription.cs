namespace BillFlow.Api.Domain;

public class Subscription : IHasConcurrencyStamp
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int PlanId { get; set; }
    public Plan Plan { get; set; } = null!;

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Trialing;

    public DateTime CurrentPeriodStart { get; set; }
    public DateTime CurrentPeriodEnd { get; set; }
    public DateTime? TrialEndsAt { get; set; }

    public bool CancelAtPeriodEnd { get; set; }
    public DateTime? CanceledAt { get; set; }

    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();

    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
