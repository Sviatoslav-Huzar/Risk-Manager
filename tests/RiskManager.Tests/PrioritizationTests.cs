using RiskManager.Core.Calculations;
using RiskManager.Core.Domain;

namespace RiskManager.Tests;

public class PrioritizationTests
{
    private const int Precision = 10;

    [Fact]
    public void Prioritize_SplitsRangeIntoThreeEqualParts()
    {
        // min = 0.1, max = 0.7, mpr = 0.2 -> bounds 0.3 and 0.5.
        double[] values = [0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7];

        PrioritizationResult<double> result = Prioritization.Prioritize(values, v => v);

        Assert.Equal(0.1, result.Min, Precision);
        Assert.Equal(0.7, result.Max, Precision);
        Assert.Equal(0.2, result.Step, Precision);
        Assert.Equal(0.3, result.LowUpper, Precision);
        Assert.Equal(0.5, result.MediumUpper, Precision);

        Assert.Equal([0.1, 0.2, 0.3], result.Low.Select(i => i.Item));
        Assert.Equal([0.4, 0.5], result.Medium.Select(i => i.Item));
        Assert.Equal([0.6, 0.7], result.High.Select(i => i.Item));
    }

    [Fact]
    public void Prioritize_BoundaryValuesGoToLowerPriority()
    {
        // min + mpr = 0.3 -> Low; min + 2mpr = 0.6 -> Medium.
        double[] values = [0.0, 0.3, 0.6, 0.9];

        PrioritizationResult<double> result = Prioritization.Prioritize(values, v => v);

        Assert.Equal(
            [PriorityLevel.Low, PriorityLevel.Low, PriorityLevel.Medium, PriorityLevel.High],
            result.Items.Select(i => i.Priority));
    }

    [Fact]
    public void Prioritize_AllValuesEqual_AllLow()
    {
        double[] values = [0.4, 0.4, 0.4];

        PrioritizationResult<double> result = Prioritization.Prioritize(values, v => v);

        Assert.Equal(0, result.Step);
        Assert.All(result.Items, i => Assert.Equal(PriorityLevel.Low, i.Priority));
    }

    [Fact]
    public void Prioritize_Empty_ReturnsEmptyResult()
    {
        PrioritizationResult<double> result = Prioritization.Prioritize(Array.Empty<double>(), v => v);

        Assert.Empty(result.Items);
    }

    [Fact]
    public void Prioritize_WorksWithRiskAssessments()
    {
        RiskAssessment[] risks =
        [
            new(AnalysisTests.Event("t1", RiskCategory.Technical), 0.9, 0.9), // vr = 0.81
            new(AnalysisTests.Event("c1", RiskCategory.Cost), 0.5, 0.2),      // vr = 0.10
            new(AnalysisTests.Event("m1", RiskCategory.Management), 0.1, 0.1) // vr = 0.01
        ];

        PrioritizationResult<RiskAssessment> result = Prioritization.Prioritize(risks, r => r.Value);

        Assert.Equal("t1", Assert.Single(result.High).Item.Event.Code);
        Assert.Empty(result.Medium);
        Assert.Equal(["c1", "m1"], result.Low.Select(i => i.Item.Event.Code));
    }
}
