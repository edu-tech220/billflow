using BillFlow.Api.Domain;
using BillFlow.Api.Services;
using BillFlow.Api.Tests.TestSupport;

namespace BillFlow.Api.Tests.Services;

public class PaymentServiceTests
{
    private static async Task<(int customerId, int planId)> SeedAsync(BillingStack stack)
    {
        var customer = new Customer { Name = "Alan Turing", Email = "alan@example.com", PasswordHash = "x", PasswordSalt = "y" };
        var plan = new Plan { Code = "starter", Name = "Starter", MonthlyPrice = 20m, TrialDays = 0 };
        stack.Db.Customers.AddRange(customer);
        stack.Db.Plans.Add(plan);
        await stack.Db.SaveChangesAsync();
        return (customer.Id, plan.Id);
    }

    [Fact]
    public async Task ChargeInvoiceAsync_ReplayedWithSameIdempotencyKey_DoesNotChargeTwice()
    {
        var gateway = new ScriptedPaymentGateway(true);
        using var stack = new BillingStack(gateway);
        var (customerId, planId) = await SeedAsync(stack);

        // CreateAsync already attempts the first charge; capture its invoice and key to replay it.
        await stack.Subscriptions.CreateAsync(customerId, planId);
        var invoice = Assert.Single(stack.Db.Invoices);
        var idempotencyKey = $"initial-{invoice.SubscriptionId}";

        var replay = await stack.Payments.ChargeInvoiceAsync(invoice.Id, idempotencyKey);

        Assert.True(replay.Succeeded);
        Assert.Equal(1, gateway.ChargeCount);
    }

    [Fact]
    public async Task ChargeInvoiceAsync_RepeatedFailures_EventuallyWritesOffAndCancelsSubscription()
    {
        // First attempt (at subscription creation) plus two dunning retries, all declined.
        var gateway = new ScriptedPaymentGateway(false, false, false);
        using var stack = new BillingStack(gateway);
        var (customerId, planId) = await SeedAsync(stack);

        var subscription = await stack.Subscriptions.CreateAsync(customerId, planId);
        var invoice = Assert.Single(stack.Db.Invoices);
        Assert.Equal(SubscriptionStatus.PastDue, subscription.Status);
        Assert.Equal(1, invoice.AttemptCount);

        await stack.Payments.ChargeInvoiceAsync(invoice.Id, "dunning-retry-1");
        await stack.Db.Entry(invoice).ReloadAsync();
        Assert.Equal(2, invoice.AttemptCount);
        Assert.Equal(InvoiceStatus.Open, invoice.Status);

        await stack.Payments.ChargeInvoiceAsync(invoice.Id, "dunning-retry-2");
        await stack.Db.Entry(invoice).ReloadAsync();
        await stack.Db.Entry(subscription).ReloadAsync();

        Assert.Equal(InvoiceStatus.Uncollectible, invoice.Status);
        Assert.Equal(SubscriptionStatus.Canceled, subscription.Status);
    }

    [Fact]
    public async Task ChargeInvoiceAsync_OnAlreadyPaidInvoice_IsANoOpSuccess()
    {
        using var stack = new BillingStack(new ScriptedPaymentGateway(true));
        var (customerId, planId) = await SeedAsync(stack);

        await stack.Subscriptions.CreateAsync(customerId, planId);
        var invoice = Assert.Single(stack.Db.Invoices);

        var result = await stack.Payments.ChargeInvoiceAsync(invoice.Id, "some-other-key");

        Assert.True(result.Succeeded);
        Assert.Equal(InvoiceStatus.Paid, result.InvoiceStatus);
    }

    [Fact]
    public async Task ChargeInvoiceAsync_OnVoidInvoice_Throws()
    {
        using var stack = new BillingStack(new ScriptedPaymentGateway(true));
        var (customerId, planId) = await SeedAsync(stack);

        await stack.Subscriptions.CreateAsync(customerId, planId);
        var invoice = Assert.Single(stack.Db.Invoices);
        invoice.Status = InvoiceStatus.Void;
        await stack.Db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidStateTransitionException>(
            () => stack.Payments.ChargeInvoiceAsync(invoice.Id, "new-key"));
    }
}
