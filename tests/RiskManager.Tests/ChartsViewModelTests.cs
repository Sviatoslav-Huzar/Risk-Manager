using RiskManager.App.ViewModels;
using RiskManager.Core.Data;

namespace RiskManager.Tests;

public class ChartsViewModelTests
{
    private readonly MainViewModel _main = new(CatalogLoader.LoadDefault());

    private ChartsViewModel Charts => _main.Charts;

    [Fact]
    public void NoRisks_ArraysAreEmptyOrZero()
    {
        Assert.Empty(Charts.RiskCodes);
        Assert.Empty(Charts.ValuesBefore);
        Assert.Empty(Charts.ValuesAfter);
        Assert.Equal([0, 0, 0, 0], Charts.GroupShares);
        Assert.Equal([0, 0, 0], Charts.PriorityCountsBefore);
        Assert.Equal([0, 0, 0], Charts.PriorityCountsAfter);
        Assert.Equal(0, Charts.MissingCount);
    }

    [Fact]
    public void Names_MatchArrays()
    {
        Assert.Equal(["Технічні", "Вартісні", "Планові", "Управлінські"], Charts.GroupNames);
        Assert.Equal(["високий", "середній", "низький"], Charts.PriorityNames);
    }

    [Fact]
    public void AnalysedRisks_AreShownBeforeAnyMeasure()
    {
        AssessThreeRisks();

        Assert.Equal(["m1", "c1", "t1"], Charts.RiskCodes);
        Assert.Equal([0.9, 0.5, 0.1], Charts.ValuesBefore.Select(value => Math.Round(value, 10)));
        Assert.Equal([null, null, null], Charts.ValuesAfter);
        Assert.Equal([1, 1, 1], Charts.PriorityCountsBefore);
        Assert.Equal(3, Charts.MissingCount);
        Assert.Equal([0.3333, 0.3333, 0, 0.3333], Charts.GroupShares.Select(share => Math.Round(share, 4)));
    }

    [Fact]
    public void EvaluatedRisk_GetsValueAtItsPosition()
    {
        AssessThreeRisks();

        Evaluate("c1", 0.5, 0.4);

        Assert.Equal([null, 0.2, null], Charts.ValuesAfter);
        Assert.Equal([0, 0, 1], Charts.PriorityCountsAfter); // one risk: max = min, i.e. low
        Assert.Equal(2, Charts.MissingCount);
    }

    [Fact]
    public void PriorityCountsAfter_FollowNewPriorities()
    {
        AssessThreeRisks();

        Evaluate("t1", 0.1, 0.1);
        Evaluate("c1", 0.1, 0.2);
        Evaluate("m1", 0.9, 0.9);

        Assert.Equal([1, 0, 2], Charts.PriorityCountsAfter);
        Assert.Equal(0, Charts.MissingCount);
    }

    [Fact]
    public void ChangedEstimate_ReordersAllRiskArraysTogether()
    {
        AssessThreeRisks();
        Evaluate("t1", 0.1, 0.1);

        _main.Estimates.Rows.Single(row => row.Code == "t1").Loss = 1;

        Assert.Equal(["t1", "m1", "c1"], Charts.RiskCodes);
        Assert.Equal(1, Charts.ValuesBefore[0], 10);
        Assert.Equal(0.01, Charts.ValuesAfter[0]!.Value, 10);
        Assert.Null(Charts.ValuesAfter[1]);
    }

    [Fact]
    public void UncheckedEvents_DisappearFromCharts()
    {
        AssessThreeRisks();
        Evaluate("t1", 0.1, 0.1);

        _main.Events.ClearAllCommand.Execute(null);

        NoRisks_ArraysAreEmptyOrZero();
    }

    [Theory]
    [InlineData(nameof(ChartsViewModel.ValuesAfter))]
    [InlineData(nameof(ChartsViewModel.PriorityCountsAfter))]
    [InlineData(nameof(ChartsViewModel.MissingCount))]
    public void Evaluation_Notifies(string propertyName)
    {
        AssessThreeRisks();

        Assert.PropertyChanged(Charts, propertyName, () => Evaluate("t1", 0.1, 0.1));
    }

    [Theory]
    [InlineData(nameof(ChartsViewModel.RiskCodes))]
    [InlineData(nameof(ChartsViewModel.ValuesBefore))]
    [InlineData(nameof(ChartsViewModel.GroupShares))]
    [InlineData(nameof(ChartsViewModel.PriorityCountsBefore))]
    public void Analysis_Notifies(string propertyName)
    {
        Assert.PropertyChanged(Charts, propertyName, AssessThreeRisks);
    }

    [Fact]
    public void NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => new ChartsViewModel(null!, _main.Monitoring));
        Assert.Throws<ArgumentNullException>(() => new ChartsViewModel(_main.Analysis, null!));
    }

    // vr: t1 = 0.1, c1 = 0.5, m1 = 0.9.
    private void AssessThreeRisks()
    {
        Assess("t1", 0.1);
        Assess("c1", 0.5);
        Assess("m1", 0.9);
    }

    private void Assess(string code, double loss)
    {
        _main.Events.Items.Single(item => item.Code == code).IsPresent = true;
        EstimateRowViewModel row = _main.Estimates.Rows.Single(r => r.Code == code);

        foreach (ExpertEstimateViewModel cell in row.Estimates)
        {
            cell.Value = 1;
        }

        row.Loss = loss;
    }

    private void Evaluate(string code, double probability, double loss)
    {
        _main.Measures.Rows.Single(row => row.Ranked.Risk.Event.Code == code).Items[0].IsSelected = true;
        MeasureEvaluationViewModel evaluation = _main.Monitoring.Rows
            .Single(row => row.Ranked.Risk.Event.Code == code).Evaluations[0];
        evaluation.Probability = probability;
        evaluation.Loss = loss;
    }
}
