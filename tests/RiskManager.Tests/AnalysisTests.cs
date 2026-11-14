using RiskManager.Core.Calculations;
using RiskManager.Core.Domain;

namespace RiskManager.Tests;

public class AnalysisTests
{
    private const int Precision = 10;

    [Fact]
    public void EventProbability_IsAverageOfExperts()
    {
        double[] estimates = [0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0];

        Assert.Equal(0.55, Analysis.EventProbability(estimates), Precision);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    public void EventProbability_EstimateOutOfRange_Throws(double bad)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Analysis.EventProbability([0.5, bad]));
    }

    [Fact]
    public void EventProbability_NoExperts_Throws()
    {
        Assert.Throws<ArgumentException>(() => Analysis.EventProbability([]));
    }

    [Theory]
    [InlineData(0.0, ProbabilityLevel.VeryLow)]
    [InlineData(0.0999, ProbabilityLevel.VeryLow)]
    [InlineData(0.10, ProbabilityLevel.Low)]       // left boundary belongs to the upper level
    [InlineData(0.2499, ProbabilityLevel.Low)]
    [InlineData(0.25, ProbabilityLevel.Medium)]
    [InlineData(0.4999, ProbabilityLevel.Medium)]
    [InlineData(0.50, ProbabilityLevel.High)]
    [InlineData(0.7499, ProbabilityLevel.High)]
    [InlineData(0.75, ProbabilityLevel.VeryHigh)]
    [InlineData(1.0, ProbabilityLevel.VeryHigh)]
    public void ClassifyProbability_Boundaries(double p, ProbabilityLevel expected)
    {
        Assert.Equal(expected, Analysis.ClassifyProbability(p));
    }

    [Fact]
    public void ClassifyProbability_IgnoresFloatingPointNoise()
    {
        // In binary floating point 0.35 - 0.1 is 0.24999999999999997, not 0.25.
        // Without rounding it would be classified as Low.
        double almostQuarter = 0.35 - 0.1;
        Assert.True(almostQuarter < 0.25);

        Assert.Equal(ProbabilityLevel.Medium, Analysis.ClassifyProbability(almostQuarter));
    }

    [Fact]
    public void RiskAssessment_ValueIsProbabilityTimesLoss()
    {
        var risk = new RiskAssessment(Event("t1", RiskCategory.Technical), 0.6, 0.5);

        Assert.Equal(0.3, risk.Value, Precision);
        Assert.Equal(ProbabilityLevel.High, risk.ProbabilityLevel);
    }

    [Theory]
    [InlineData(1.5, 0.5)]
    [InlineData(0.5, -0.5)]
    public void RiskAssessment_OutOfRange_Throws(double probability, double loss)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RiskAssessment(Event("t1", RiskCategory.Technical), probability, loss));
    }

    [Fact]
    public void CategoryProbabilities_SharesAndMeans()
    {
        RiskAssessment[] risks =
        [
            new(Event("t1", RiskCategory.Technical), 0.2, 0.5),
            new(Event("t2", RiskCategory.Technical), 0.4, 0.5),
            new(Event("c1", RiskCategory.Cost), 0.6, 0.5),
            new(Event("m1", RiskCategory.Management), 0.8, 0.5),
        ];

        IReadOnlyList<CategoryProbability> result = Analysis.CategoryProbabilities(risks);
        CategoryProbability technical = result.Single(c => c.Category == RiskCategory.Technical);
        CategoryProbability schedule = result.Single(c => c.Category == RiskCategory.Schedule);

        // Sum of all er = 2.0; technical sum = 0.6.
        Assert.Equal(0.3, technical.Share, Precision);
        Assert.Equal(0.3, technical.Mean, Precision);
        Assert.Equal(2, technical.EventCount);
        Assert.Equal(ProbabilityLevel.Medium, technical.MeanLevel);

        Assert.Equal(0, schedule.EventCount);
        Assert.Equal(0, schedule.Share);

        Assert.Equal(1.0, result.Sum(c => c.Share), Precision);
    }

    [Fact]
    public void CategoryProbabilities_AllZero_DoesNotDivideByZero()
    {
        RiskAssessment[] risks = [new(Event("t1", RiskCategory.Technical), 0, 0)];

        IReadOnlyList<CategoryProbability> result = Analysis.CategoryProbabilities(risks);

        Assert.All(result, c => Assert.Equal(0, c.Share));
    }

    internal static RiskEvent Event(string code, RiskCategory category) =>
        new(code, category, $"Event {code}");
}
