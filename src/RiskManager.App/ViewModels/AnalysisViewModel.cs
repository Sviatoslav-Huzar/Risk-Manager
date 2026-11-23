using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using RiskManager.Core.Calculations;

namespace RiskManager.App.ViewModels;

/// <summary>
/// The "Analysis" tab: the probabilities of the risk groups (formula 5.6) and
/// the priorities of the risks (stage 2.4). Read-only: everything is calculated
/// from the assessments of the "Expert estimates" tab.
/// </summary>
public partial class AnalysisViewModel : ObservableObject
{
    private readonly EstimatesViewModel _estimates;

    public AnalysisViewModel(EstimatesViewModel estimates)
    {
        ArgumentNullException.ThrowIfNull(estimates);
        _estimates = estimates;
        _estimates.PropertyChanged += OnEstimatesPropertyChanged;

        Recalculate();
    }

    /// <summary>ptc, pcc, ppc and pmc — always all four groups.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<CategoryProbability> Groups { get; private set; }

    /// <summary>min, max, mpr and the sets HER, MER and LER.</summary>
    [ObservableProperty]
    public partial PrioritizationResult<RiskAssessment> Priorities { get; private set; }

    /// <summary>
    /// The risks from the greatest vr to the smallest. A new list every time
    /// rather than an ObservableCollection: one changed number can move min or
    /// max and with them the priorities and positions of all risks.
    /// </summary>
    [ObservableProperty]
    public partial IReadOnlyList<RankedRisk> Risks { get; private set; }

    /// <summary>
    /// How many checked events are not analysed because their estimates are
    /// incomplete. Without it a partial analysis would look like a full one.
    /// </summary>
    [ObservableProperty]
    public partial int MissingCount { get; private set; }

    private void OnEstimatesPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EstimatesViewModel.Assessments))
        {
            Recalculate();
        }
    }

    // The attribute tells the compiler that the constructor, which calls this
    // method, leaves no property null.
    [MemberNotNull(nameof(Groups), nameof(Priorities), nameof(Risks))]
    private void Recalculate()
    {
        IReadOnlyList<RiskAssessment> assessments = _estimates.Assessments;

        Groups = Analysis.CategoryProbabilities(assessments);
        Priorities = Prioritization.Prioritize(assessments, risk => risk.Value);

        // OrderByDescending is stable: equal risks stay in catalog order.
        Risks = Priorities.Items
            .OrderByDescending(item => item.Value)
            .Select((item, index) => new RankedRisk(index + 1, item.Item, item.Priority))
            .ToList();

        MissingCount = _estimates.Rows.Count - assessments.Count;
    }
}
