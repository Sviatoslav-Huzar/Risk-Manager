using RiskManager.App.ViewModels;
using RiskManager.Core.Data;

namespace RiskManager.Tests;

public class MainViewModelTests
{
    [Fact]
    public void Constructor_CreatesSourcesTabFromCatalog()
    {
        var vm = new MainViewModel(CatalogLoader.LoadDefault());

        Assert.NotNull(vm.Sources);
        Assert.Equal(18, vm.Sources.Items.Count);
    }

    [Fact]
    public void Constructor_CreatesEventsTabFromCatalog()
    {
        var vm = new MainViewModel(CatalogLoader.LoadDefault());

        Assert.Equal(46, vm.Events.Items.Count);
        Assert.Equal([11, 8, 11, 16], vm.Events.Groups.Select(g => g.CategoryCount));
    }

    [Fact]
    public void SourcesAndEvents_AreIndependent()
    {
        // Sources and events share codes (both have "t1"), so this makes sure
        // the two tabs do not accidentally share state.
        var vm = new MainViewModel(CatalogLoader.LoadDefault());

        vm.Sources.Items.Single(item => item.Code == "t1").IsPresent = true;

        Assert.Equal(1.0 / 18, vm.Sources.Total, 10);
        Assert.Equal(0, vm.Events.Total);
        Assert.All(vm.Events.Items, item => Assert.False(item.IsPresent));
    }

    [Fact]
    public void Constructor_NullCatalog_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new MainViewModel(null!));
    }
}
