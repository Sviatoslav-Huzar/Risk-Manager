using RiskManager.App.ViewModels;
using RiskManager.Core.Data;
using RiskManager.Core.Domain;

namespace RiskManager.Tests;

public class ChecklistViewModelTests
{
    private const int Precision = 10;

    private readonly ChecklistViewModel _vm = new(CatalogLoader.LoadDefault().Sources);

    [Fact]
    public void InitialState_NothingSelected()
    {
        Assert.Equal(18, _vm.Items.Count);
        Assert.All(_vm.Items, item => Assert.False(item.IsPresent));
        Assert.Equal(0, _vm.Total);
    }

    [Fact]
    public void Groups_FollowMethodologyOrderAndSizes()
    {
        Assert.Equal(
            [RiskCategory.Technical, RiskCategory.Cost, RiskCategory.Schedule, RiskCategory.Management],
            _vm.Groups.Select(g => g.Category));

        Assert.Equal([7, 3, 3, 5], _vm.Groups.Select(g => g.CategoryCount));
    }

    [Fact]
    public void CheckingSource_UpdatesTotalAndGroup()
    {
        Item("t1").IsPresent = true;

        Assert.Equal(1.0 / 18, _vm.Total, Precision);

        ChecklistGroupViewModel technical = Group(RiskCategory.Technical);
        Assert.Equal(1, technical.PresentCount);
        Assert.Equal(1.0 / 18, technical.Share, Precision);

        Assert.Equal(0, Group(RiskCategory.Cost).PresentCount);
    }

    [Fact]
    public void UncheckingSource_RevertsValues()
    {
        Item("c2").IsPresent = true;
        Item("c2").IsPresent = false;

        Assert.Equal(0, _vm.Total);
        Assert.Equal(0, Group(RiskCategory.Cost).PresentCount);
    }

    [Fact]
    public void CheckingSource_RaisesPropertyChangedForTotal()
    {
        // This is the event WPF bindings listen to: without it the screen
        // would keep showing the old value even though the property changed.
        Assert.PropertyChanged(_vm, nameof(ChecklistViewModel.Total),
            () => Item("m3").IsPresent = true);
    }

    [Fact]
    public void CheckingSource_RaisesPropertyChangedForGroupShare()
    {
        ChecklistGroupViewModel management = Group(RiskCategory.Management);

        Assert.PropertyChanged(management, nameof(ChecklistGroupViewModel.Share),
            () => Item("m3").IsPresent = true);
    }

    [Fact]
    public void SelectAll_SelectsEverything()
    {
        _vm.SelectAllCommand.Execute(null);

        Assert.All(_vm.Items, item => Assert.True(item.IsPresent));
        Assert.Equal(1.0, _vm.Total, Precision);
        Assert.Equal(7, Group(RiskCategory.Technical).PresentCount);
    }

    [Fact]
    public void ClearAll_ClearsEverything()
    {
        _vm.SelectAllCommand.Execute(null);

        _vm.ClearAllCommand.Execute(null);

        Assert.All(_vm.Items, item => Assert.False(item.IsPresent));
        Assert.Equal(0, _vm.Total);
    }

    [Fact]
    public void Commands_CanExecuteReflectsState()
    {
        // Nothing selected: only "Select all" makes sense.
        Assert.True(_vm.SelectAllCommand.CanExecute(null));
        Assert.False(_vm.ClearAllCommand.CanExecute(null));

        Item("t1").IsPresent = true;
        Assert.True(_vm.SelectAllCommand.CanExecute(null));
        Assert.True(_vm.ClearAllCommand.CanExecute(null));

        _vm.SelectAllCommand.Execute(null);
        Assert.False(_vm.SelectAllCommand.CanExecute(null));
        Assert.True(_vm.ClearAllCommand.CanExecute(null));
    }

    [Fact]
    public void Commands_RaiseCanExecuteChanged()
    {
        // WPF re-reads CanExecute (and enables/disables the button) only
        // when this event fires.
        bool raised = false;
        _vm.ClearAllCommand.CanExecuteChanged += (_, _) => raised = true;

        Item("t1").IsPresent = true;

        Assert.True(raised);
    }

    private ChecklistItemViewModel Item(string code) =>
        _vm.Items.Single(item => item.Code == code);

    private ChecklistGroupViewModel Group(RiskCategory category) =>
        _vm.Groups.Single(group => group.Category == category);
}
