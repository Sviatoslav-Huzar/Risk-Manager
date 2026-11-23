using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using RiskManager.Core.Domain;

namespace RiskManager.App.ViewModels;

/// <summary>
/// The "Expert estimates" tab (stages 2.1-2.3). Shows one row per risk event
/// that is checked on the "Risk events" tab and follows its changes.
/// </summary>
public partial class EstimatesViewModel : ObservableObject
{
    private readonly ChecklistViewModel _events;

    // Rows exist for all events from the start, whether visible or not:
    // unchecking an event hides its row but keeps the entered numbers.
    private readonly Dictionary<ChecklistItemViewModel, EstimateRowViewModel> _rowsByItem = [];

    public EstimatesViewModel(ChecklistViewModel events)
    {
        ArgumentNullException.ThrowIfNull(events);
        _events = events;

        foreach (ChecklistItemViewModel item in events.Items)
        {
            RiskEvent riskEvent = item.Item as RiskEvent
                ?? throw new ArgumentException("The checklist must contain risk events.", nameof(events));

            var row = new EstimateRowViewModel(riskEvent);
            row.PropertyChanged += OnRowPropertyChanged;
            _rowsByItem.Add(item, row);

            if (item.IsPresent)
            {
                Rows.Add(row);
            }

            item.PropertyChanged += OnItemPropertyChanged;
        }
    }

    /// <summary>
    /// The rows of the checked events in catalog order. ObservableCollection
    /// reports every added or removed row, so the table updates itself.
    /// </summary>
    public ObservableCollection<EstimateRowViewModel> Rows { get; } = [];

    /// <summary>How many of the visible rows are filled in completely.</summary>
    [ObservableProperty]
    public partial int CompleteCount { get; private set; }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ChecklistItemViewModel.IsPresent)
            || sender is not ChecklistItemViewModel item)
        {
            return;
        }

        EstimateRowViewModel row = _rowsByItem[item];

        if (item.IsPresent)
        {
            // The new row goes after the rows of all checked events that
            // precede it in the catalog.
            int index = _events.Items
                .TakeWhile(other => other != item)
                .Count(other => other.IsPresent);

            Rows.Insert(index, row);
        }
        else
        {
            Rows.Remove(row);
        }

        UpdateCompleteCount();
    }

    private void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EstimateRowViewModel.IsComplete))
        {
            UpdateCompleteCount();
        }
    }

    private void UpdateCompleteCount()
    {
        CompleteCount = Rows.Count(row => row.IsComplete);
    }
}
