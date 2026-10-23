using NetCafe.Data;

namespace NetCafe.Data.Tests;

public sealed class SessionCostCalculatorTests
{
    [Fact]
    public void UsesReportedAmountWhenTheDatabaseHasClosedValue()
    {
        var result = SessionCostCalculator.EstimatePlayAmount(12000, 60, 15000);

        Assert.Equal(15000, result);
    }

    [Fact]
    public void EstimatesActiveSessionFromHourlyRateAndMinutes()
    {
        var result = SessionCostCalculator.EstimatePlayAmount(12000, 45, 0);

        Assert.Equal(9000, result);
    }

    [Fact]
    public void ReturnsZeroWhenThereIsNoActiveSessionTime()
    {
        var result = SessionCostCalculator.EstimatePlayAmount(12000, null, 0);

        Assert.Equal(0, result);
    }
}
