namespace BillFlow.Api.Services;

public record GatewayChargeResult(bool Succeeded, string? FailureReason);

/// <summary>Abstraction over the external card network/processor BillFlow would charge in production.</summary>
public interface IPaymentGateway
{
    Task<GatewayChargeResult> ChargeAsync(decimal amount, string customerEmail, CancellationToken ct = default);
}
