namespace BillFlow.Api.Services;

/// <summary>
/// The retry schedule for failed invoice payments: retry after 1, 3 and 7 days: beyond that,
/// the invoice is written off and its subscription is canceled.
/// </summary>
public static class DunningPolicy
{
    private static readonly int[] RetryOffsetDays = { 1, 3, 7 };

    public static int MaxAttempts => RetryOffsetDays.Length;

    public static DateTime? NextRetryAt(int attemptCount, DateTime from) =>
        attemptCount >= 1 && attemptCount <= RetryOffsetDays.Length
            ? from.AddDays(RetryOffsetDays[attemptCount - 1])
            : null;

    public static bool ShouldWriteOff(int attemptCount) => attemptCount >= RetryOffsetDays.Length;
}
