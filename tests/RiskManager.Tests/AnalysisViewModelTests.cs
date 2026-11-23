using RiskManager.App.ViewModels;
using RiskManager.Core.Data;
using RiskManager.Core.Domain;

namespace RiskManager.Tests;

public class AnalysisViewModelTests
{
    private const int Precision = 10;

    private readonly ChecklistViewModel _events = new(CatalogLoader.LoadDefault().Events);
    private readonly EstimatesViewModel _estimates;
    private readonly AnalysisViewModel _vm;

    public AnalysisViewModelTests()
    {
        _estimates = new EstimatesViewModel(_events);
        _vm = new AnalysisViewModel(_estimates);
    }

    [Fact]
    public void NoEstimates_EverythingIsEmptyOrZero()
    {
        Assert.Empty(_vm.Risks);
        Assert.Equal(0, _vm.Priorities.Min);
        Assert.Equal(0, _vm.Priorities.Max);
        Assert.Equal(0, _vm.MissingCount);
        Assert.Equal(4, _vm.Groups.Count);
        Assert.All(_vm.Groups, group => Assert.Equal(0, group.Share));
    }

    [Fact]
    public void ExistingAssessments_AreAnalysedInConstructor()
    {
        Assess("t1", 0.5, 0.5);

        var vm = new AnalysisViewModel(_estimates);

        Assert.Equal("t1", Assert.Single(vm.Risks).Risk.Event.Code);
    }

    [Fact]
    public void Risks_AreRankedByValueAndPrioritized()
    {
        // vr: 0.01, 0.5, 0.9; mpr = 0.8900 / 3, the bounds are about 0.307 and 0.603.
        Assess("t1", 0.1, 0.1);
        Assess("c1", 1.0, 0.5);
        Assess("m1", 1.0, 0.9);

        Assert.Equal(["m1", "c1", "t1"], Codes());
        Assert.Equal([1, 2, 3], _vm.Risks.Select(risk => risk.Rank));
        Assert.Equal(
            [PriorityLevel.High, PriorityLevel.Medium, PriorityLevel.Low],
            _vm.Risks.Select(risk => risk.Priority));
        Assert.Equal(0.01, _vm.Priorities.Min, Precision);
        Assert.Equal(0.9, _vm.Priorities.Max, Precision);
    }

    [Fact]
    public void EqualRisks_KeepCatalogOrderAndAreAllLow()
    {
        Assess("m1", 0.5, 0.5);
        Assess("t2", 0.5, 0.5);
        Assess("t1", 0.5, 0.5);

        Assert.Equal(["t1", "t2", "m1"], Codes());
        Assert.All(_vm.Risks, risk => Assert.Equal(PriorityLevel.Low, risk.Priority));
    }

    [Fact]
    public void ChangedNumber_RecalculatesRanks()
    {
        Assess("t1", 0.5, 0.2);
        Assess("c1", 0.5, 0.6);
        Assert.Equal(["c1", "t1"], Codes());

        Row("t1").Loss = 1;

        Assert.Equal(["t1", "c1"], Codes());
        Assert.Equal(0.5, _vm.Risks[0].Risk.Value, Precision);
    }

    [Fact]
    public void UncheckedEvent_DisappearsAndReturns()
    {
        Assess("t1", 0.5, 0.2);
        Assess("c1", 0.5, 0.6);

        Item("c1").IsPresent = false;
        Assert.Equal(["t1"], Codes());

        Item("c1").IsPresent = true;
        Assert.Equal(["c1", "t1"], Codes());
    }

    [Fact]
    public void Groups_SharesFollowFormula()
    {
        Assess("t1", 0.6, 0.5);
        Assess("t2", 0.2, 0.5);
        Assess("m1", 0.2, 0.5);

        Assert.Equal([0.8, 0.0, 0.0, 0.2], _vm.Groups.Select(group => Math.Round(group.Share, Precision)));
        Assert.Equal(0.4, _vm.Groups[0].Mean, Precision);
        Assert.Equal(ProbabilityLevel.Medium, _vm.Groups[0].MeanLevel);
    }

    [Fact]
    public void MissingCount_CountsIncompleteRows()
    {
        Assess("t1", 0.5, 0.5);
        Item("c1").IsPresent = true;
        Item("c2").IsPresent = true;
        Assert.Equal(2, _vm.MissingCount);

        Row("t1").Loss = null;
        Assert.Equal(3, _vm.MissingCount);

        _events.ClearAllCommand.Execute(null);
        Assert.Equal(0, _vm.MissingCount);
    }

    [Theory]
    [InlineData(nameof(AnalysisViewModel.Groups))]
    [InlineData(nameof(AnalysisViewModel.Priorities))]
    [InlineData(nameof(AnalysisViewModel.Risks))]
    [InlineData(nameof(AnalysisViewModel.MissingCount))]
    public void CompletingRow_NotifiesAboutEveryProperty(string propertyName)
    {
        Item("t1").IsPresent = true;

        Assert.PropertyChanged(_vm, propertyName, () => Assess("t1", 0.5, 0.5));
    }

    [Fact]
    public void NullEstimates_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new AnalysisViewModel(null!));
    }

    private void Assess(string code, double probability, double loss)
    {
        Item(code).IsPresent = true;

        foreach (ExpertEstimateViewModel cell in Row(code).Estimates)
        {
            cell.Value = probability;
        }

        Row(code).Loss = loss;
    }

    private IEnumerable<string> Codes() => _vm.Risks.Select(risk => risk.Risk.Event.Code);

    private EstimateRowViewModel Row(string code) => _estimates.Rows.Single(row => row.Code == code);

    private ChecklistItemViewModel Item(string code) => _events.Items.Single(item => item.Code == code);
}
