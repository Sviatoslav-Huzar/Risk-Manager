using RiskManager.App.ViewModels;
using RiskManager.Core.Data;

namespace RiskManager.Tests;

public class EstimatesViewModelTests
{
    private const int Seed = 42;

    private readonly ChecklistViewModel _events = new(CatalogLoader.LoadDefault().Events);
    private readonly EstimatesViewModel _vm;

    public EstimatesViewModelTests()
    {
        _vm = new EstimatesViewModel(_events, new Random(Seed));
    }

    [Fact]
    public void NothingChecked_NoRows()
    {
        Assert.Empty(_vm.Rows);
        Assert.Equal(0, _vm.CompleteCount);
    }

    [Fact]
    public void AlreadyCheckedEvents_GetRowsInConstructor()
    {
        Check("c1");

        var vm = new EstimatesViewModel(_events);

        Assert.Equal("c1", Assert.Single(vm.Rows).Code);
    }

    [Fact]
    public void CheckingEvents_AddsRowsInCatalogOrder()
    {
        Check("m1");
        Check("t1");
        Check("c2");

        Assert.Equal(["t1", "c2", "m1"], _vm.Rows.Select(row => row.Code));
    }

    [Fact]
    public void UncheckingEvent_RemovesOnlyItsRow()
    {
        Check("t1");
        Check("c2");

        Uncheck("t1");

        Assert.Equal(["c2"], _vm.Rows.Select(row => row.Code));
    }

    [Fact]
    public void RecheckingEvent_RestoresEnteredValues()
    {
        Check("t1");
        FillCompletely(_vm.Rows[0]);

        Uncheck("t1");
        Assert.Equal(0, _vm.CompleteCount);

        Check("t1");
        Assert.Equal(0.5, _vm.Rows[0].Probability);
        Assert.Equal(1, _vm.CompleteCount);
    }

    [Fact]
    public void SelectAllAndClearAll_AreFollowed()
    {
        _events.SelectAllCommand.Execute(null);
        Assert.Equal(_events.Items.Select(item => item.Code), _vm.Rows.Select(row => row.Code));

        _events.ClearAllCommand.Execute(null);
        Assert.Empty(_vm.Rows);
    }

    [Fact]
    public void CompleteCount_FollowsRows()
    {
        Check("t1");
        Check("c2");

        Assert.PropertyChanged(_vm, nameof(EstimatesViewModel.CompleteCount),
            () => FillCompletely(_vm.Rows[0]));
        Assert.Equal(1, _vm.CompleteCount);

        _vm.Rows[0].Loss = null;
        Assert.Equal(0, _vm.CompleteCount);
    }

    [Fact]
    public void Assessments_ContainOnlyVisibleCompleteRowsInCatalogOrder()
    {
        Check("m1");
        Check("t1");
        Check("c2");
        FillCompletely(_vm.Rows[2]);
        FillCompletely(_vm.Rows[0]);
        Assert.Equal(["t1", "m1"], _vm.Assessments.Select(risk => risk.Event.Code));

        Uncheck("t1");
        Assert.Equal(["m1"], _vm.Assessments.Select(risk => risk.Event.Code));
    }

    [Fact]
    public void Assessments_FollowChangesOfRowThatStaysComplete()
    {
        Check("t1");
        FillCompletely(_vm.Rows[0]);

        Assert.PropertyChanged(_vm, nameof(EstimatesViewModel.Assessments),
            () => _vm.Rows[0].Loss = 0.2);

        Assert.Equal(0.1, Assert.Single(_vm.Assessments).Value, 10);
    }

    [Fact]
    public void FillRandom_CompletesAllVisibleRowsWithValidValues()
    {
        _events.SelectAllCommand.Execute(null);

        _vm.FillRandomCommand.Execute(null);

        Assert.Equal(46, _vm.CompleteCount);
        Assert.All(_vm.Rows.SelectMany(row => row.Estimates), cell => Assert.InRange(cell.Value!.Value, 0, 1));
        Assert.All(_vm.Rows, row => Assert.InRange(row.Loss!.Value, 0, 1));
    }

    [Fact]
    public void FillRandom_ExpertsRoughlyAgree()
    {
        _events.SelectAllCommand.Execute(null);

        _vm.FillRandomCommand.Execute(null);

        // Each expert deviates by at most 0.1, so two experts differ by at most
        // 0.2, plus 0.01 for rounding both values to two decimals.
        Assert.All(_vm.Rows, row => Assert.InRange(
            row.Estimates.Max(cell => cell.Value!.Value) - row.Estimates.Min(cell => cell.Value!.Value),
            0, 0.2101));
    }

    [Fact]
    public void FillRandom_KeepsEnteredValues()
    {
        Check("t1");
        _vm.Rows[0].Estimates[0].Value = 0.123;
        _vm.Rows[0].Loss = 0.456;

        _vm.FillRandomCommand.Execute(null);

        Assert.Equal(0.123, _vm.Rows[0].Estimates[0].Value);
        Assert.Equal(0.456, _vm.Rows[0].Loss);
        Assert.True(_vm.Rows[0].IsComplete);
    }

    [Fact]
    public void FillRandom_DoesNotTouchHiddenRows()
    {
        Check("t1");
        _vm.FillRandomCommand.Execute(null);

        Check("c1");

        Assert.All(_vm.Rows[1].Estimates, cell => Assert.Null(cell.Value));
        Assert.Null(_vm.Rows[1].Loss);
    }

    [Fact]
    public void FillRandom_SameSeed_GivesSameNumbers()
    {
        Check("t1");
        var other = new EstimatesViewModel(_events, new Random(Seed));

        _vm.FillRandomCommand.Execute(null);
        other.FillRandomCommand.Execute(null);

        Assert.Equal(
            _vm.Rows[0].Estimates.Select(cell => cell.Value),
            other.Rows[0].Estimates.Select(cell => cell.Value));
        Assert.Equal(_vm.Rows[0].Loss, other.Rows[0].Loss);
    }

    [Fact]
    public void FillRandom_IsEnabledOnlyWhileSomethingIsIncomplete()
    {
        bool raised = false;
        _vm.FillRandomCommand.CanExecuteChanged += (_, _) => raised = true;
        Assert.False(_vm.FillRandomCommand.CanExecute(null));

        Check("t1");
        Assert.True(raised);
        Assert.True(_vm.FillRandomCommand.CanExecute(null));

        raised = false;
        _vm.FillRandomCommand.Execute(null);
        Assert.True(raised);
        Assert.False(_vm.FillRandomCommand.CanExecute(null));
    }

    [Fact]
    public void ChecklistOfSources_Throws()
    {
        var sources = new ChecklistViewModel(CatalogLoader.LoadDefault().Sources);

        Assert.Throws<ArgumentException>(() => new EstimatesViewModel(sources));
    }

    [Fact]
    public void NullChecklist_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new EstimatesViewModel(null!));
    }

    private void Check(string code) => Item(code).IsPresent = true;

    private void Uncheck(string code) => Item(code).IsPresent = false;

    private ChecklistItemViewModel Item(string code) =>
        _events.Items.Single(item => item.Code == code);

    private static void FillCompletely(EstimateRowViewModel row)
    {
        foreach (ExpertEstimateViewModel cell in row.Estimates)
        {
            cell.Value = 0.5;
        }

        row.Loss = 0.5;
    }
}
