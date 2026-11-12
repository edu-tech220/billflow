using BillFlow.Api.Services;

namespace BillFlow.Api.Tests.Services;

public class DunningPolicyTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 3)]
    [InlineData(3, 7)]
    public void NextRetryAt_FollowsTheBackoffSchedule(int attemptCount, int expectedOffsetDays)
    {
        var from = new DateTime(2026, 1, 1);

        var next = DunningPolicy.NextRetryAt(attemptCount, from);

        Assert.Equal(from.AddDays(expectedOffsetDays), next);
    }

    [Fact]
    public void NextRetryAt_BeyondMaxAttempts_ReturnsNull()
    {
        var next = DunningPolicy.NextRetryAt(DunningPolicy.MaxAttempts + 1, DateTime.UtcNow);

        Assert.Null(next);
    }

    [Fact]
    public void ShouldWriteOff_OnlyOnceMaxAttemptsReached()
    {
        Assert.False(DunningPolicy.ShouldWriteOff(DunningPolicy.MaxAttempts - 1));
        Assert.True(DunningPolicy.ShouldWriteOff(DunningPolicy.MaxAttempts));
    }
}
