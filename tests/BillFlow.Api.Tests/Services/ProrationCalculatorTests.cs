using BillFlow.Api.Services;

namespace BillFlow.Api.Tests.Services;

public class ProrationCalculatorTests
{
    [Fact]
    public void Upgrade_Halfway_Through_Period_ChargesHalfTheDifference()
    {
        var periodStart = new DateTime(2026, 1, 1);
        var periodEnd = new DateTime(2026, 1, 31);
        var changeDate = new DateTime(2026, 1, 16);

        var net = ProrationCalculator.CalculateNetCharge(
            oldPlanMonthlyPrice: 10m, newPlanMonthlyPrice: 30m, periodStart, periodEnd, changeDate);

        // 15 of 30 days remain: (30-10) * 15/30 = 10
        Assert.Equal(10.00m, net);
    }

    [Fact]
    public void Downgrade_Halfway_Through_Period_ProducesNegativeNetCharge()
    {
        var periodStart = new DateTime(2026, 1, 1);
        var periodEnd = new DateTime(2026, 1, 31);
        var changeDate = new DateTime(2026, 1, 16);

        var net = ProrationCalculator.CalculateNetCharge(
            oldPlanMonthlyPrice: 30m, newPlanMonthlyPrice: 10m, periodStart, periodEnd, changeDate);

        Assert.Equal(-10.00m, net);
    }

    [Fact]
    public void ChangeDate_AtPeriodEnd_ChargesNothing()
    {
        var periodStart = new DateTime(2026, 1, 1);
        var periodEnd = new DateTime(2026, 1, 31);

        var net = ProrationCalculator.CalculateNetCharge(10m, 50m, periodStart, periodEnd, periodEnd);

        Assert.Equal(0m, net);
    }

    [Fact]
    public void ChangeDate_AtPeriodStart_ChargesFullDifference()
    {
        var periodStart = new DateTime(2026, 1, 1);
        var periodEnd = new DateTime(2026, 1, 31);

        var net = ProrationCalculator.CalculateNetCharge(10m, 50m, periodStart, periodEnd, periodStart);

        Assert.Equal(40.00m, net);
    }
}
