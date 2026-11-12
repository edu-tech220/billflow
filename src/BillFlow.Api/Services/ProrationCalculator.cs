namespace BillFlow.Api.Services;

public static class ProrationCalculator
{
    /// <summary>
    /// Computes the net amount owed when switching plans mid-cycle: the new plan's cost for the
    /// remaining days minus a credit for the unused days on the old plan, both prorated by the
    /// fraction of the billing period left. Positive means the customer owes more immediately;
    /// negative means they get credit toward their next renewal.
    /// </summary>
    public static decimal CalculateNetCharge(
        decimal oldPlanMonthlyPrice,
        decimal newPlanMonthlyPrice,
        DateTime periodStart,
        DateTime periodEnd,
        DateTime changeDate)
    {
        var totalDays = (periodEnd - periodStart).TotalDays;
        if (totalDays <= 0)
        {
            return 0m;
        }

        var clampedChangeDate = changeDate < periodStart ? periodStart : changeDate > periodEnd ? periodEnd : changeDate;
        var remainingDays = (periodEnd - clampedChangeDate).TotalDays;
        var remainingFraction = (decimal)(remainingDays / totalDays);

        var unusedCredit = Math.Round(oldPlanMonthlyPrice * remainingFraction, 2, MidpointRounding.AwayFromZero);
        var newPlanCharge = Math.Round(newPlanMonthlyPrice * remainingFraction, 2, MidpointRounding.AwayFromZero);

        return newPlanCharge - unusedCredit;
    }
}
