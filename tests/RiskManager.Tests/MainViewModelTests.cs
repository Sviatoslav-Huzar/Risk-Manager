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
    public void Constructor_NullCatalog_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new MainViewModel(null!));
    }
}
