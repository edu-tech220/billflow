using BillFlow.Api.Domain;

namespace BillFlow.Api.Services;

public interface ISubscriptionService
{
    Task<Subscription> CreateAsync(int customerId, int planId, CancellationToken ct = default);

    Task<Subscription> ChangePlanAsync(int subscriptionId, int newPlanId, CancellationToken ct = default);

    Task<Subscription> CancelAsync(int subscriptionId, bool atPeriodEnd, CancellationToken ct = default);

    /// <summary>Rolls a subscription into its next billing period, issuing and attempting to collect the renewal invoice.</summary>
    Task<Subscription> RenewAsync(int subscriptionId, CancellationToken ct = default);
}
