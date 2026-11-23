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

        Sources = new ChecklistViewModel(catalog.Sources);
        Events = new ChecklistViewModel(catalog.Events);

        // The only place where tabs are connected: the estimates follow the
        // events, but know nothing about MainViewModel or other tabs.
        Estimates = new EstimatesViewModel(Events);
    }

    /// <summary>The "Risk sources" tab (stage 1.1).</summary>
    public ChecklistViewModel Sources { get; }

    /// <summary>The "Risk events" tab (stage 1.2).</summary>
    public ChecklistViewModel Events { get; }

    /// <summary>The "Expert estimates" tab (stages 2.1-2.3).</summary>
    public EstimatesViewModel Estimates { get; }
}
