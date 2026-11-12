using BillFlow.Api.Domain;
using BillFlow.Api.Services;

namespace BillFlow.Api.Tests.Services;

public class SubscriptionTransitionsTests
{
    [Theory]
    [InlineData(SubscriptionStatus.Trialing, SubscriptionStatus.Active, true)]
    [InlineData(SubscriptionStatus.Trialing, SubscriptionStatus.Canceled, true)]
    [InlineData(SubscriptionStatus.Active, SubscriptionStatus.PastDue, true)]
    [InlineData(SubscriptionStatus.PastDue, SubscriptionStatus.Active, true)]
    [InlineData(SubscriptionStatus.PastDue, SubscriptionStatus.Expired, true)]
    [InlineData(SubscriptionStatus.Canceled, SubscriptionStatus.Active, false)]
    [InlineData(SubscriptionStatus.Expired, SubscriptionStatus.Active, false)]
    [InlineData(SubscriptionStatus.Trialing, SubscriptionStatus.PastDue, false)]
    public void CanTransition_MatchesExpectedLifecycle(SubscriptionStatus from, SubscriptionStatus to, bool expected)
    {
        Assert.Equal(expected, SubscriptionTransitions.CanTransition(from, to));
    }

    [Fact]
    public void EnsureTransition_OnIllegalMove_Throws()
    {
        Assert.Throws<InvalidStateTransitionException>(
            () => SubscriptionTransitions.EnsureTransition(SubscriptionStatus.Canceled, SubscriptionStatus.Active));
    }
}
