using BillFlow.Api.Data;
using BillFlow.Api.Services;

namespace BillFlow.Api.Tests.TestSupport;

/// <summary>Wires the full chain of billing services against one SQLite context, for service-level tests.</summary>
public sealed class BillingStack : IDisposable
{
    private readonly SqliteDbContextFactory _factory;

    public BillFlowDbContext Db { get; }
    public TestClock Clock { get; }
    public IPaymentGateway Gateway { get; }
    public ISubscriptionService Subscriptions { get; }
    public IBillingService Billing { get; }
    public IPaymentService Payments { get; }

    public BillingStack(IPaymentGateway? gateway = null)
    {
        _factory = new SqliteDbContextFactory();
        Db = _factory.CreateContext();
        Clock = new TestClock();
        Gateway = gateway ?? new ScriptedPaymentGateway();

        var invoiceNumbers = new InvoiceNumberGenerator(Db);
        Billing = new BillingService(Db, invoiceNumbers, Clock);
        Payments = new PaymentService(Db, Gateway, Clock);
        Subscriptions = new SubscriptionService(Db, Billing, Payments, Clock);
    }

    public void Dispose()
    {
        Db.Dispose();
        _factory.Dispose();
    }
}
