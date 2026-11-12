namespace BillFlow.Api.Domain;

public class Customer : IHasConcurrencyStamp
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public CustomerRole Role { get; set; } = CustomerRole.Customer;

    /// <summary>
    /// Credit accumulated from downgrade prorations, applied to the next renewal invoice.
    /// </summary>
    public decimal CreditBalance { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();

    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
