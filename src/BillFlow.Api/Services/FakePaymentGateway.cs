namespace BillFlow.Api.Services;

/// <summary>
/// Stands in for a real payment processor in this portfolio build: every charge succeeds.
/// Tests exercise the failure/dunning path by substituting their own IPaymentGateway.
/// </summary>
public class FakePaymentGateway : IPaymentGateway
{
    public Task<GatewayChargeResult> ChargeAsync(decimal amount, string customerEmail, CancellationToken ct = default) =>
        Task.FromResult(new GatewayChargeResult(Succeeded: true, FailureReason: null));
}
