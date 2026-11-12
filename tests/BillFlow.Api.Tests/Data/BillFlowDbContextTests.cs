using BillFlow.Api.Domain;
using BillFlow.Api.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace BillFlow.Api.Tests.Data;

public class BillFlowDbContextTests
{
    [Fact]
    public async Task SavingRelatedEntities_PersistsAcrossContextInstances()
    {
        using var factory = new SqliteDbContextFactory();

        await using (var writeContext = factory.CreateContext())
        {
            var plan = new Plan { Code = "pro", Name = "Pro", MonthlyPrice = 29.00m, TrialDays = 14 };
            var customer = new Customer { Name = "Ada Lovelace", Email = "ada@example.com", PasswordHash = "x", PasswordSalt = "y" };
            writeContext.Plans.Add(plan);
            writeContext.Customers.Add(customer);
            await writeContext.SaveChangesAsync();

            writeContext.Subscriptions.Add(new Subscription
            {
                CustomerId = customer.Id,
                PlanId = plan.Id,
                Status = SubscriptionStatus.Active,
                CurrentPeriodStart = new DateTime(2026, 1, 1),
                CurrentPeriodEnd = new DateTime(2026, 2, 1),
            });
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = factory.CreateContext();
        var persisted = await readContext.Subscriptions
            .Include(s => s.Customer)
            .Include(s => s.Plan)
            .SingleAsync();

        Assert.Equal("Ada Lovelace", persisted.Customer.Name);
        Assert.Equal("pro", persisted.Plan.Code);
        Assert.Equal(SubscriptionStatus.Active, persisted.Status);
    }

    [Fact]
    public async Task DuplicateCustomerEmail_ViolatesUniqueIndex()
    {
        using var factory = new SqliteDbContextFactory();
        await using var context = factory.CreateContext();

        context.Customers.Add(new Customer { Name = "First", Email = "dup@example.com", PasswordHash = "x", PasswordSalt = "y" });
        await context.SaveChangesAsync();

        context.Customers.Add(new Customer { Name = "Second", Email = "dup@example.com", PasswordHash = "x", PasswordSalt = "y" });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task ConcurrentUpdate_WithStaleConcurrencyStamp_ThrowsConcurrencyException()
    {
        using var factory = new SqliteDbContextFactory();

        int planId;
        await using (var seedContext = factory.CreateContext())
        {
            var plan = new Plan { Code = "starter", Name = "Starter", MonthlyPrice = 9.00m, TrialDays = 7 };
            seedContext.Plans.Add(plan);
            await seedContext.SaveChangesAsync();
            planId = plan.Id;
        }

        await using var firstContext = factory.CreateContext();
        var firstLoad = await firstContext.Plans.SingleAsync(p => p.Id == planId);

        await using var secondContext = factory.CreateContext();
        var secondLoad = await secondContext.Plans.SingleAsync(p => p.Id == planId);
        secondLoad.MonthlyPrice = 10.00m;
        await secondContext.SaveChangesAsync();

        firstLoad.MonthlyPrice = 11.00m;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => firstContext.SaveChangesAsync());
    }
}
