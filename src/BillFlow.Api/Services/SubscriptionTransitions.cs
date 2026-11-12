using BillFlow.Api.Domain;

namespace BillFlow.Api.Services;

/// <summary>
/// The subscription lifecycle state machine. Trialing/Active subscriptions pay their way
/// forward; once Canceled or Expired a subscription is terminal — reactivating means
/// creating a brand new subscription, not resurrecting the old one.
/// </summary>
public static class SubscriptionTransitions
{
    private static readonly Dictionary<SubscriptionStatus, SubscriptionStatus[]> Allowed = new()
    {
        [SubscriptionStatus.Trialing] = new[] { SubscriptionStatus.Active, SubscriptionStatus.Canceled },
        [SubscriptionStatus.Active] = new[] { SubscriptionStatus.PastDue, SubscriptionStatus.Canceled },
        [SubscriptionStatus.PastDue] = new[] { SubscriptionStatus.Active, SubscriptionStatus.Canceled, SubscriptionStatus.Expired },
        [SubscriptionStatus.Canceled] = Array.Empty<SubscriptionStatus>(),
        [SubscriptionStatus.Expired] = Array.Empty<SubscriptionStatus>(),
    };

    public static bool CanTransition(SubscriptionStatus from, SubscriptionStatus to) =>
        Allowed.TryGetValue(from, out var targets) && targets.Contains(to);

    public static void EnsureTransition(SubscriptionStatus from, SubscriptionStatus to)
    {
        if (!CanTransition(from, to))
        {
            throw new InvalidStateTransitionException($"Cannot move a subscription from {from} to {to}.");
        }
    }
}
