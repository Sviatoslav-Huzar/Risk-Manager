using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using RiskManager.App.Converters;
using RiskManager.Core.Domain;

namespace RiskManager.App.ViewModels;

/// <summary>
/// The "Charts" tab. Prepares plain labels and numbers; which chart shows them
/// and how it looks is decided in ChartsView.xaml, so this class does not depend
/// on the chart library.
/// </summary>
public partial class ChartsViewModel : ObservableObject
{
    private static readonly PriorityLevel[] PriorityOrder =
        [PriorityLevel.High, PriorityLevel.Medium, PriorityLevel.Low];

    private readonly AnalysisViewModel _analysis;
    private readonly MonitoringViewModel _monitoring;

    public ChartsViewModel(AnalysisViewModel analysis, MonitoringViewModel monitoring)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(monitoring);

        _analysis = analysis;
        _monitoring = monitoring;
        _analysis.PropertyChanged += OnAnalysisPropertyChanged;
        _monitoring.PropertyChanged += OnMonitoringPropertyChanged;

        GroupNames = Enum.GetValues<RiskCategory>().Select(CategoryNameConverter.GetName).ToArray();
        PriorityNames = PriorityOrder.Select(PriorityLevelConverter.GetName).ToArray();

        Recalculate();
    }

    public string[] GroupNames { get; }

    /// <summary>ptc, pcc, ppc and pmc (formula 5.6).</summary>
    [ObservableProperty]
    public partial double[] GroupShares { get; private set; }

    /// <summary>High, medium, low — the order of all priority arrays.</summary>
    public string[] PriorityNames { get; }

    /// <summary>The sizes of HER, MER and LER before the measures.</summary>
    [ObservableProperty]
    public partial int[] PriorityCountsBefore { get; private set; }

    /// <summary>The same after the measures; only evaluated risks are counted.</summary>
    [ObservableProperty]
    public partial int[] PriorityCountsAfter { get; private set; }

    /// <summary>The codes of all analysed risks, the greatest first.</summary>
    [ObservableProperty]
    public partial string[] RiskCodes { get; private set; }

    /// <summary>vr of every risk.</summary>
    [ObservableProperty]
    public partial double[] ValuesBefore { get; private set; }

    /// <summary>evr of every risk; null (no column) if the risk is not evaluated.</summary>
    [ObservableProperty]
    public partial double?[] ValuesAfter { get; private set; }

    /// <summary>How many risks have no "after" value.</summary>
    [ObservableProperty]
    public partial int MissingCount { get; private set; }

    private void OnAnalysisPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AnalysisViewModel.Risks))
        {
            Recalculate();
        }
    }

    private void OnMonitoringPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MonitoringViewModel.Results))
        {
            Recalculate();
        }
    }

    [MemberNotNull(nameof(GroupShares), nameof(PriorityCountsBefore), nameof(PriorityCountsAfter))]
    [MemberNotNull(nameof(RiskCodes), nameof(ValuesBefore), nameof(ValuesAfter))]
    private void Recalculate()
    {
        GroupShares = _analysis.Groups.Select(group => group.Share).ToArray();

        PriorityCountsBefore = CountPriorities(_analysis.Risks.Select(risk => risk.Priority));
        PriorityCountsAfter = CountPriorities(_monitoring.Results.Items.Select(item => item.Priority));

        Dictionary<RiskEvent, double> valuesAfter = _monitoring.Results.Items
            .ToDictionary(item => item.Item.Risk.Event, item => item.Value);

        RiskCodes = _analysis.Risks.Select(risk => risk.Risk.Event.Code).ToArray();
        ValuesBefore = _analysis.Risks.Select(risk => risk.Risk.Value).ToArray();
        ValuesAfter = _analysis.Risks
            .Select(risk => valuesAfter.TryGetValue(risk.Risk.Event, out double value) ? value : (double?)null)
            .ToArray();

        MissingCount = ValuesAfter.Count(value => value is null);
    }

    private static int[] CountPriorities(IEnumerable<PriorityLevel> priorities)
    {
        List<PriorityLevel> list = priorities.ToList();

        return PriorityOrder.Select(level => list.Count(priority => priority == level)).ToArray();
    }
}
