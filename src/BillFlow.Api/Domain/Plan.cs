namespace BillFlow.Api.Domain;

public class Plan : IHasConcurrencyStamp
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public int TrialDays { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();

    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
