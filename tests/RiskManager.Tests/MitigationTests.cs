using RiskManager.Core.Calculations;
using RiskManager.Core.Domain;

namespace RiskManager.Tests;

public class MitigationTests
{
    private const int Precision = 10;

    private static readonly Measure Training = new("ev1", "Training");
    private static readonly Measure Consultant = new("ev7", "Consultant");
    private static readonly Measure Insurance = new("ev9", "Insurance");

    private readonly RiskAssessment _risk =
        new(AnalysisTests.Event("m6", RiskCategory.Management), 0.8, 0.5); // vr = 0.4

    [Fact]
    public void ChooseBest_PicksSmallestValueAfterMeasure()
    {
        MeasureOption[] options =
        [
            new(Training, 0.4, 0.5),    // evr = 0.20
            new(Consultant, 0.5, 0.2),  // evr = 0.10  <- best
            new(Insurance, 0.8, 0.25),  // evr = 0.20
        ];

        MitigationResult result = Mitigation.ChooseBest(_risk, options);

        Assert.Equal("ev7", result.Chosen.Measure.Code);
        Assert.Equal(0.3, result.Reduction, Precision);
        Assert.Equal(0.75, result.ReductionRatio, Precision);
    }

    [Fact]
    public void ChooseBest_Tie_FirstOptionWins()
    {
        MeasureOption[] options =
        [
            new(Training, 0.4, 0.5),   // 0.2
            new(Insurance, 0.8, 0.25), // 0.2
        ];

        Assert.Equal("ev1", Mitigation.ChooseBest(_risk, options).Chosen.Measure.Code);
    }

    [Fact]
    public void ChooseBest_NoOptions_Throws()
    {
        Assert.Throws<ArgumentException>(() => Mitigation.ChooseBest(_risk, []));
    }

    [Fact]
    public void ReductionRatio_ZeroRisk_IsZero()
    {
        var zeroRisk = new RiskAssessment(AnalysisTests.Event("t1", RiskCategory.Technical), 0, 0.5);

        MitigationResult result = Mitigation.ChooseBest(zeroRisk, [new(Training, 0, 0)]);

        Assert.Equal(0, result.ReductionRatio);
    }

    [Fact]
    public void Reprioritize_AfterMeasures()
    {
        // Stage 4: priorities are recalculated using evr instead of vr.
        MitigationResult[] results =
        [
            Mitigation.ChooseBest(_risk, [new(Consultant, 0.5, 0.2)]),                 // evr = 0.10
            Mitigation.ChooseBest(
                new RiskAssessment(AnalysisTests.Event("t1", RiskCategory.Technical), 0.9, 0.9),
                [new(Training, 0.9, 0.9)]),                                             // evr = 0.81
            Mitigation.ChooseBest(
                new RiskAssessment(AnalysisTests.Event("c1", RiskCategory.Cost), 0.3, 0.3),
                [new(Insurance, 0.1, 0.1)]),                                            // evr = 0.01
        ];

        PrioritizationResult<MitigationResult> after =
            Prioritization.Prioritize(results, r => r.Chosen.Value);

        Assert.Equal("t1", Assert.Single(after.High).Item.Risk.Event.Code);
    }
}
