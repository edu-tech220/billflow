using BillFlow.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.Api.Data;

public class BillFlowDbContext : DbContext
{
    public BillFlowDbContext(DbContextOptions<BillFlowDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<InvoiceSequence> InvoiceSequences => Set<InvoiceSequence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasIndex(c => c.Email).IsUnique();
            entity.Property(c => c.Name).IsRequired().HasMaxLength(200);
            entity.Property(c => c.Email).IsRequired().HasMaxLength(256);
            entity.Property(c => c.CreditBalance).HasColumnType("decimal(18,2)");
            entity.Property(c => c.ConcurrencyStamp).IsConcurrencyToken();
        });

        modelBuilder.Entity<Plan>(entity =>
        {
            entity.HasIndex(p => p.Code).IsUnique();
            entity.Property(p => p.Code).IsRequired().HasMaxLength(50);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.MonthlyPrice).HasColumnType("decimal(18,2)");
            entity.Property(p => p.ConcurrencyStamp).IsConcurrencyToken();
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasOne(s => s.Customer)
                .WithMany(c => c.Subscriptions)
                .HasForeignKey(s => s.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.Plan)
                .WithMany(p => p.Subscriptions)
                .HasForeignKey(s => s.PlanId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(s => s.ConcurrencyStamp).IsConcurrencyToken();
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasIndex(i => i.Number).IsUnique();
            entity.Property(i => i.Number).IsRequired().HasMaxLength(30);
            entity.Property(i => i.Amount).HasColumnType("decimal(18,2)");

            entity.HasOne(i => i.Subscription)
                .WithMany(s => s.Invoices)
                .HasForeignKey(i => i.SubscriptionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(i => i.ConcurrencyStamp).IsConcurrencyToken();
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasIndex(p => p.IdempotencyKey).IsUnique();
            entity.Property(p => p.IdempotencyKey).IsRequired().HasMaxLength(100);
            entity.Property(p => p.Amount).HasColumnType("decimal(18,2)");

            entity.HasOne(p => p.Invoice)
                .WithMany(i => i.Payments)
                .HasForeignKey(p => p.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InvoiceSequence>(entity =>
        {
            entity.HasKey(s => s.Year);
            entity.Property(s => s.ConcurrencyStamp).IsConcurrencyToken();
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        RefreshConcurrencyStamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        RefreshConcurrencyStamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void RefreshConcurrencyStamps()
    {
        foreach (var entry in ChangeTracker.Entries<IHasConcurrencyStamp>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.ConcurrencyStamp = Guid.NewGuid();
            }
        }
    }
}
