using RiskManager.App.ViewModels;
using RiskManager.Core.Data;
using RiskManager.Core.Domain;

namespace RiskManager.Tests;

public class MeasuresViewModelTests
{
    private readonly RiskCatalog _catalog = CatalogLoader.LoadDefault();
    private readonly ChecklistViewModel _events;
    private readonly EstimatesViewModel _estimates;
    private readonly AnalysisViewModel _analysis;
    private readonly MeasuresViewModel _vm;

    public MeasuresViewModelTests()
    {
        _events = new ChecklistViewModel(_catalog.Events);
        _estimates = new EstimatesViewModel(_events);
        _analysis = new AnalysisViewModel(_estimates);
        _vm = new MeasuresViewModel(_analysis, _catalog.Measures);
    }

    [Fact]
    public void NoRisks_NoRowsAndNoSelection()
    {
        Assert.Empty(_vm.Rows);
        Assert.Null(_vm.SelectedRow);
        Assert.Equal(0, _vm.PlannedCount);
    }

    [Fact]
    public void Rows_FollowRankingAndGreatestRiskIsSelected()
    {
        Assess("t1", loss: 0.2);
        Assess("c1", loss: 0.6);

        Assert.Equal(["c1", "t1"], Codes());
        // t1 was selected while it was alone, but nobody clicked it.
        Assert.Same(Row("c1"), _vm.SelectedRow);
    }

    [Fact]
    public void ExistingRisks_GetRowsInConstructor()
    {
        Assess("t1", loss: 0.2);

        var vm = new MeasuresViewModel(_analysis, _catalog.Measures);

        Assert.Same(Assert.Single(vm.Rows), vm.SelectedRow);
    }

    [Fact]
    public void MarkedMeasures_AreListedInCatalogOrder()
    {
        Assess("t1", loss: 0.2);
        RiskMeasuresViewModel row = Row("t1");
        Assert.False(row.HasMeasures);
        Assert.Equal("", row.SelectedCodes);

        Mark(row, "ev7");
        Mark(row, "ev2");

        Assert.Equal(["ev2", "ev7"], row.SelectedMeasures.Select(measure => measure.Code));
        Assert.Equal("ev2, ev7", row.SelectedCodes);
        Assert.True(row.HasMeasures);
    }

    [Theory]
    [InlineData(nameof(RiskMeasuresViewModel.SelectedMeasures))]
    [InlineData(nameof(RiskMeasuresViewModel.SelectedCodes))]
    [InlineData(nameof(RiskMeasuresViewModel.HasMeasures))]
    public void Marking_NotifiesAboutDerivedProperties(string propertyName)
    {
        Assess("t1", loss: 0.2);

        Assert.PropertyChanged(Row("t1"), propertyName, () => Mark(Row("t1"), "ev1"));
    }

    [Fact]
    public void Marks_OfDifferentRisksAreIndependent()
    {
        Assess("t1", loss: 0.2);
        Assess("c1", loss: 0.6);

        Mark(Row("t1"), "ev1");

        Assert.False(Row("c1").HasMeasures);
    }

    [Fact]
    public void PlannedCount_CountsRisksNotMeasures()
    {
        Assess("t1", loss: 0.2);
        Assess("c1", loss: 0.6);

        Assert.PropertyChanged(_vm, nameof(MeasuresViewModel.PlannedCount), () => Mark(Row("t1"), "ev1"));
        Mark(Row("t1"), "ev2");
        Assert.Equal(1, _vm.PlannedCount);

        foreach (MeasureItemViewModel item in Row("t1").Items)
        {
            item.IsSelected = false;
        }

        Assert.Equal(0, _vm.PlannedCount);
    }

    [Fact]
    public void ChangedEstimate_ReordersRowsButKeepsMarksAndSelection()
    {
        Assess("t1", loss: 0.2);
        Assess("c1", loss: 0.6);
        RiskMeasuresViewModel row = Row("t1");
        Mark(row, "ev1");
        _vm.SelectedRow = row;

        Estimate("t1").Loss = 1;

        Assert.Equal(["t1", "c1"], Codes());
        Assert.Same(row, _vm.Rows[0]);
        Assert.Same(row, _vm.SelectedRow);
        Assert.Equal("ev1", row.SelectedCodes);
        Assert.Equal(1, row.Ranked.Rank);
        Assert.Equal(1, row.Ranked.Risk.Loss);
        Assert.Equal(PriorityLevel.High, row.Ranked.Priority);
    }

    [Fact]
    public void ViewThatResetsSelection_DoesNotLoseIt()
    {
        // Imitates a table that clears its selection when it gets a new list.
        Assess("t1", loss: 0.2);
        Assess("c1", loss: 0.6);
        _vm.SelectedRow = Row("t1");
        RiskMeasuresViewModel selected = Row("t1");
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MeasuresViewModel.Rows))
            {
                _vm.SelectedRow = null;
            }
        };

        Estimate("t1").Loss = 1;

        Assert.Same(selected, _vm.SelectedRow);
    }

    [Fact]
    public void UncheckedEvent_ReturnsWithSelectionAndMarks()
    {
        Assess("t1", loss: 0.2);
        Assess("c1", loss: 0.6);
        Mark(Row("t1"), "ev1");
        _vm.SelectedRow = Row("t1");

        Event("t1").IsPresent = false;
        Assert.Equal(["c1"], Codes());
        Assert.Same(Row("c1"), _vm.SelectedRow);
        Assert.Equal(0, _vm.PlannedCount);

        Event("t1").IsPresent = true;
        Assert.Equal("ev1", Row("t1").SelectedCodes);
        Assert.Equal(1, _vm.PlannedCount);
        Assert.Same(Row("t1"), _vm.SelectedRow);
    }

    [Fact]
    public void NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => new MeasuresViewModel(null!, _catalog.Measures));
        Assert.Throws<ArgumentNullException>(() => new MeasuresViewModel(_analysis, null!));
    }

    private void Assess(string code, double loss)
    {
        Event(code).IsPresent = true;

        foreach (ExpertEstimateViewModel cell in Estimate(code).Estimates)
        {
            cell.Value = 0.5;
        }

        Estimate(code).Loss = loss;
    }

    private static void Mark(RiskMeasuresViewModel row, string measureCode) =>
        row.Items.Single(item => item.Code == measureCode).IsSelected = true;

    private IEnumerable<string> Codes() => _vm.Rows.Select(row => row.Ranked.Risk.Event.Code);

    private RiskMeasuresViewModel Row(string code) =>
        _vm.Rows.Single(row => row.Ranked.Risk.Event.Code == code);

    private EstimateRowViewModel Estimate(string code) => _estimates.Rows.Single(row => row.Code == code);

    private ChecklistItemViewModel Event(string code) => _events.Items.Single(item => item.Code == code);
}
