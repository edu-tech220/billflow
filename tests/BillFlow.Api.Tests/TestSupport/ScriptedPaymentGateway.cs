using BillFlow.Api.Services;

namespace BillFlow.Api.Tests.TestSupport;

/// <summary>Test double whose charge outcomes are scripted up front, so dunning/retry paths are reproducible.</summary>
public class ScriptedPaymentGateway : IPaymentGateway
{
    private readonly Queue<bool> _outcomes;

    public ScriptedPaymentGateway(params bool[] outcomes)
    {
        _outcomes = new Queue<bool>(outcomes);
    }

    public int ChargeCount { get; private set; }

    public Task<GatewayChargeResult> ChargeAsync(decimal amount, string customerEmail, CancellationToken ct = default)
    {
        ChargeCount++;
        var succeeded = _outcomes.Count > 0 ? _outcomes.Dequeue() : true;
        return Task.FromResult(succeeded
            ? new GatewayChargeResult(true, null)
            : new GatewayChargeResult(false, "card_declined"));
    }
}
