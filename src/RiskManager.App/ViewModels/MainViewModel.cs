using CommunityToolkit.Mvvm.ComponentModel;
using RiskManager.Core.Domain;

namespace RiskManager.App.ViewModels;

/// <summary>
/// The view model of the main window — the root of the view model tree.
/// Holds one view model per tab; more tabs will be added in later iterations.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    public MainViewModel(RiskCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        Sources = new SourcesViewModel(catalog.Sources);
    }

    /// <summary>The "Risk sources" tab (stage 1.1).</summary>
    public SourcesViewModel Sources { get; }
}
