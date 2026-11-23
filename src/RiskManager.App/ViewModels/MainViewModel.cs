using CommunityToolkit.Mvvm.ComponentModel;
using RiskManager.Core.Domain;

namespace RiskManager.App.ViewModels;

/// <summary>
/// The view model of the main window — the root of the view model tree.
/// Holds one view model per tab.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    public MainViewModel(RiskCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        Sources = new ChecklistViewModel(catalog.Sources);
        Events = new ChecklistViewModel(catalog.Events);

        // The only place where tabs are connected. The dependencies go one way,
        // Events -> Estimates -> Analysis -> Measures -> Monitoring: a tab knows
        // only its source.
        Estimates = new EstimatesViewModel(Events);
        Analysis = new AnalysisViewModel(Estimates);
        Measures = new MeasuresViewModel(Analysis, catalog.Measures);
        Monitoring = new MonitoringViewModel(Measures);
        Charts = new ChartsViewModel(Analysis, Monitoring);
    }

    /// <summary>The "Risk sources" tab (stage 1.1).</summary>
    public ChecklistViewModel Sources { get; }

    /// <summary>The "Risk events" tab (stage 1.2).</summary>
    public ChecklistViewModel Events { get; }

    /// <summary>The "Expert estimates" tab (stages 2.1-2.3).</summary>
    public EstimatesViewModel Estimates { get; }

    /// <summary>The "Analysis" tab (formula 5.6 and stage 2.4).</summary>
    public AnalysisViewModel Analysis { get; }

    /// <summary>The "Measures" tab (stage 3).</summary>
    public MeasuresViewModel Measures { get; }

    /// <summary>The "Monitoring" tab (stage 4).</summary>
    public MonitoringViewModel Monitoring { get; }

    /// <summary>The "Charts" tab.</summary>
    public ChartsViewModel Charts { get; }
}
