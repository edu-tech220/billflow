using BillFlow.Api.Domain;
using BillFlow.Api.Services;
using BillFlow.Api.Tests.TestSupport;

namespace BillFlow.Api.Tests.Services;

public class SubscriptionServiceTests
{
    private static async Task<(int customerId, int trialPlanId, int paidPlanId, int otherPaidPlanId)> SeedAsync(BillingStack stack)
    {
        var customer = new Customer { Name = "Grace Hopper", Email = "grace@example.com", PasswordHash = "x", PasswordSalt = "y" };
        var trialPlan = new Plan { Code = "trial", Name = "Trial", MonthlyPrice = 20m, TrialDays = 14 };
        var paidPlan = new Plan { Code = "starter", Name = "Starter", MonthlyPrice = 20m, TrialDays = 0 };
        var otherPaidPlan = new Plan { Code = "pro", Name = "Pro", MonthlyPrice = 50m, TrialDays = 0 };

        stack.Db.Customers.Add(customer);
        stack.Db.Plans.AddRange(trialPlan, paidPlan, otherPaidPlan);
        await stack.Db.SaveChangesAsync();

        return (customer.Id, trialPlan.Id, paidPlan.Id, otherPaidPlan.Id);
    }

    [Fact]
    public async Task CreateAsync_WithTrialPlan_StartsTrialingWithoutInvoicing()
    {
        using var stack = new BillingStack();
        var (customerId, trialPlanId, _, _) = await SeedAsync(stack);

        var subscription = await stack.Subscriptions.CreateAsync(customerId, trialPlanId);

        Assert.Equal(SubscriptionStatus.Trialing, subscription.Status);
        Assert.NotNull(subscription.TrialEndsAt);
        Assert.Empty(stack.Db.Invoices);
    }

    [Fact]
    public async Task CreateAsync_WithPaidPlan_ChargesFirstInvoiceImmediately()
    {
        using var stack = new BillingStack();
        var (customerId, _, paidPlanId, _) = await SeedAsync(stack);

        var subscription = await stack.Subscriptions.CreateAsync(customerId, paidPlanId);

        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        var invoice = Assert.Single(stack.Db.Invoices);
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        Assert.Equal(20m, invoice.Amount);
    }

    [Fact]
    public async Task CreateAsync_WhenCustomerAlreadyHasOpenSubscription_Throws()
    {
        using var stack = new BillingStack();
        var (customerId, trialPlanId, paidPlanId, _) = await SeedAsync(stack);
        await stack.Subscriptions.CreateAsync(customerId, trialPlanId);

        await Assert.ThrowsAsync<InvalidStateTransitionException>(
            () => stack.Subscriptions.CreateAsync(customerId, paidPlanId));
    }

    [Fact]
    public async Task ChangePlanAsync_Upgrade_ChargesProratedDifferenceImmediately()
    {
        using var stack = new BillingStack();
        var (customerId, _, paidPlanId, otherPaidPlanId) = await SeedAsync(stack);
        var subscription = await stack.Subscriptions.CreateAsync(customerId, paidPlanId);
        stack.Clock.UtcNow = subscription.CurrentPeriodStart.AddDays(15);

        var updated = await stack.Subscriptions.ChangePlanAsync(subscription.Id, otherPaidPlanId);

        Assert.Equal(otherPaidPlanId, updated.PlanId);
        var prorationInvoice = Assert.Single(stack.Db.Invoices, i => i.Type == InvoiceType.Proration);
        Assert.Equal(InvoiceStatus.Paid, prorationInvoice.Status);
        Assert.True(prorationInvoice.Amount > 0);
    }

    [Fact]
    public async Task ChangePlanAsync_Downgrade_CreditsCustomerInsteadOfInvoicing()
    {
        using var stack = new BillingStack();
        var (customerId, _, paidPlanId, otherPaidPlanId) = await SeedAsync(stack);
        var subscription = await stack.Subscriptions.CreateAsync(customerId, otherPaidPlanId);
        stack.Clock.UtcNow = subscription.CurrentPeriodStart.AddDays(15);

        await stack.Subscriptions.ChangePlanAsync(subscription.Id, paidPlanId);

        Assert.DoesNotContain(stack.Db.Invoices, i => i.Type == InvoiceType.Proration);
        var customer = await stack.Db.Customers.FindAsync(customerId);
        Assert.True(customer!.CreditBalance > 0);
    }

    [Fact]
    public async Task CancelAsync_Immediate_MarksSubscriptionCanceled()
    {
        using var stack = new BillingStack();
        var (customerId, trialPlanId, _, _) = await SeedAsync(stack);
        var subscription = await stack.Subscriptions.CreateAsync(customerId, trialPlanId);

        var canceled = await stack.Subscriptions.CancelAsync(subscription.Id, atPeriodEnd: false);

        Assert.Equal(SubscriptionStatus.Canceled, canceled.Status);
        Assert.NotNull(canceled.CanceledAt);
    }

    [Fact]
    public async Task CancelAsync_AtPeriodEnd_KeepsSubscriptionActiveUntilRenewal()
    {
        using var stack = new BillingStack();
        var (customerId, trialPlanId, _, _) = await SeedAsync(stack);
        var subscription = await stack.Subscriptions.CreateAsync(customerId, trialPlanId);

        var result = await stack.Subscriptions.CancelAsync(subscription.Id, atPeriodEnd: true);
        Assert.Equal(SubscriptionStatus.Trialing, result.Status);
        Assert.True(result.CancelAtPeriodEnd);

        stack.Clock.UtcNow = result.CurrentPeriodEnd.AddMinutes(1);
        var renewed = await stack.Subscriptions.RenewAsync(subscription.Id);

        Assert.Equal(SubscriptionStatus.Canceled, renewed.Status);
    }

    [Fact]
    public async Task RenewAsync_AfterTrial_ActivatesAndChargesFirstRenewalInvoice()
    {
        using var stack = new BillingStack();
        var (customerId, trialPlanId, _, _) = await SeedAsync(stack);
        var subscription = await stack.Subscriptions.CreateAsync(customerId, trialPlanId);
        var trialEnd = subscription.CurrentPeriodEnd;

        stack.Clock.UtcNow = trialEnd.AddMinutes(1);
        var renewed = await stack.Subscriptions.RenewAsync(subscription.Id);

        Assert.Equal(SubscriptionStatus.Active, renewed.Status);
        var invoice = Assert.Single(stack.Db.Invoices);
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        Assert.Equal(trialEnd, renewed.CurrentPeriodStart);
    }

    [Fact]
    public async Task RenewAsync_WithDecliningGateway_MovesSubscriptionToPastDue()
    {
        using var stack = new BillingStack(gateway: new ScriptedPaymentGateway(false));
        var (customerId, _, paidPlanId, _) = await SeedAsync(stack);

        // The initial charge at creation time also goes through the declining gateway.
        var subscription = await stack.Subscriptions.CreateAsync(customerId, paidPlanId);

        Assert.Equal(SubscriptionStatus.PastDue, subscription.Status);
        var invoice = Assert.Single(stack.Db.Invoices);
        Assert.Equal(InvoiceStatus.Open, invoice.Status);
        Assert.NotNull(invoice.NextRetryAt);
    }
}
