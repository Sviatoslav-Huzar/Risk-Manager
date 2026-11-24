using CommunityToolkit.Mvvm.ComponentModel;
using RiskManager.Core.Data;
using RiskManager.Core.Domain;

namespace RiskManager.App.ViewModels;

/// <summary>
/// One open project: the root of the view models of all tabs. Opening another
/// project creates another object, so nothing of the previous one can remain.
/// </summary>
public sealed class ProjectViewModel : ObservableObject
{
    private readonly RiskCatalog _catalog;

    /// <param name="catalog">The reference data.</param>
    /// <param name="saved">The data of a project file; nothing means a new project.</param>
    public ProjectViewModel(RiskCatalog catalog, RiskProject? saved = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        _catalog = catalog;
        saved ??= RiskProject.Empty;

        Sources = new ChecklistViewModel(catalog.Sources, saved.Sources);
        Events = new ChecklistViewModel(catalog.Events, saved.Events);

        // The only place where tabs are connected. The dependencies go one way,
        // Events -> Estimates -> Analysis -> Measures -> Monitoring: a tab knows
        // only its source.
        Estimates = new EstimatesViewModel(Events, saved: saved.Risks);
        Analysis = new AnalysisViewModel(Estimates);
        Measures = new MeasuresViewModel(Analysis, catalog.Measures, saved.Risks);
        Monitoring = new MonitoringViewModel(Measures, saved: saved.Risks);
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

    /// <summary>
    /// Collects everything the user has entered. Risks that are hidden at the
    /// moment (unchecked or incomplete) are included as well.
    /// </summary>
    public RiskProject ToProject()
    {
        List<RiskData> risks = [];

        foreach (EstimateRowViewModel row in Estimates.AllRows)
        {
            var risk = new RiskData(
                row.Code,
                row.Estimates.Select(estimate => estimate.Value).ToList(),
                row.Loss,
                Measures.GetMarkedCodes(row.Event),
                GetEvaluations(row.Event));

            bool isEmpty = risk.Estimates.All(estimate => estimate is null)
                && risk.Loss is null
                && risk.Measures.Count == 0
                && risk.Evaluations.Count == 0;

            if (!isEmpty)
            {
                risks.Add(risk);
            }
        }

        return new RiskProject(RiskProject.CurrentVersion, Sources.PresentCodes, Events.PresentCodes, risks);
    }

    // In catalog order, whatever the order in which the measures were marked:
    // equal projects must produce equal files.
    private List<EvaluationData> GetEvaluations(RiskEvent riskEvent)
    {
        Dictionary<string, EvaluationData> byMeasure = Monitoring.GetEvaluations(riskEvent)
            .Where(evaluation => evaluation.Probability is not null || evaluation.Loss is not null)
            .ToDictionary(evaluation => evaluation.Measure);

        return _catalog.Measures
            .Where(measure => byMeasure.ContainsKey(measure.Code))
            .Select(measure => byMeasure[measure.Code])
            .ToList();
    }
}
