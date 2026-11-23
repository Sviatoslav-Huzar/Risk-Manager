using RiskManager.App.ViewModels;
using RiskManager.Core.Data;

namespace RiskManager.Tests;

public class EstimatesViewModelTests
{
    private readonly ChecklistViewModel _events = new(CatalogLoader.LoadDefault().Events);
    private readonly EstimatesViewModel _vm;

    public EstimatesViewModelTests()
    {
        _vm = new EstimatesViewModel(_events);
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
