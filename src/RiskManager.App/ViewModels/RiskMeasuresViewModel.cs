using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using RiskManager.Core.Domain;

namespace RiskManager.App.ViewModels;

/// <summary>
/// One risk of the "Measures" tab together with its own list of 19 check boxes.
/// </summary>
public partial class RiskMeasuresViewModel : ObservableObject
{
    /// <param name="ranked">The analysed risk.</param>
    /// <param name="measures">All measures of the catalog.</param>
    /// <param name="markedCodes">Codes of the measures that are marked from the start.</param>
    public RiskMeasuresViewModel(
        RankedRisk ranked,
        IReadOnlyList<Measure> measures,
        IReadOnlyCollection<string>? markedCodes = null)
    {
        ArgumentNullException.ThrowIfNull(ranked);
        ArgumentNullException.ThrowIfNull(measures);

        Ranked = ranked;
        Items = measures
            .Select(measure => new MeasureItemViewModel(measure)
            {
                IsSelected = markedCodes?.Contains(measure.Code) ?? false,
            })
            .ToList();

        foreach (MeasureItemViewModel item in Items)
        {
            item.PropertyChanged += OnItemPropertyChanged;
        }

        UpdateSelectedMeasures();
    }

    /// <summary>
    /// The current rank, value and priority. The estimates can change at any
    /// time, so <see cref="MeasuresViewModel"/> replaces this snapshot, while
    /// the check boxes below stay as they are.
    /// </summary>
    [ObservableProperty]
    public partial RankedRisk Ranked { get; internal set; }

    /// <summary>All measures of the catalog.</summary>
    public IReadOnlyList<MeasureItemViewModel> Items { get; }

    /// <summary>The candidates for the best measure, in catalog order.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMeasures))]
    [NotifyPropertyChangedFor(nameof(SelectedCodes))]
    public partial IReadOnlyList<Measure> SelectedMeasures { get; private set; }

    public bool HasMeasures => SelectedMeasures.Count > 0;

    /// <summary>The candidates as one string for the table, e.g. "ev1, ev7".</summary>
    public string SelectedCodes => string.Join(", ", SelectedMeasures.Select(measure => measure.Code));

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MeasureItemViewModel.IsSelected))
        {
            UpdateSelectedMeasures();
        }
    }

    [MemberNotNull(nameof(SelectedMeasures))]
    private void UpdateSelectedMeasures()
    {
        SelectedMeasures = Items
            .Where(item => item.IsSelected)
            .Select(item => item.Measure)
            .ToList();
    }
}
