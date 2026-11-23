using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RiskManager.Core.Calculations;
using RiskManager.Core.Domain;

namespace RiskManager.App.ViewModels;

/// <summary>
/// The "Risk sources" tab (stage 1.1): 18 check boxes grouped by category
/// and the live result of formulas (5.2)-(5.3).
/// </summary>
public partial class SourcesViewModel : ObservableObject
{
    private readonly IReadOnlyList<RiskSource> _sources;

    public SourcesViewModel(IReadOnlyList<RiskSource> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);
        _sources = sources;

        Items = sources
            .Select(source => new SourceItemViewModel(source))
            .ToList();

        // Enum.GetValues keeps the methodology order (t, c, p, m) and creates
        // a group for every category — the same approach as in Identification.
        Groups = Enum.GetValues<RiskCategory>()
            .Select(category => new SourceGroupViewModel(
                category,
                Items.Where(item => item.Category == category).ToList()))
            .ToList();

        // Every check box reports its changes; the tab reacts by recalculating.
        foreach (SourceItemViewModel item in Items)
        {
            item.PropertyChanged += OnItemPropertyChanged;
        }

        Recalculate();
    }

    /// <summary>All sources in catalog order.</summary>
    public IReadOnlyList<SourceItemViewModel> Items { get; }

    /// <summary>The four category blocks.</summary>
    public IReadOnlyList<SourceGroupViewModel> Groups { get; }

    /// <summary>R = tc + cc + pc + mc (formula 5.3).</summary>
    [ObservableProperty]
    public partial double Total { get; private set; }

    [RelayCommand(CanExecute = nameof(CanSelectAll))]
    private void SelectAll()
    {
        // Each assignment triggers a recalculation (18 in total). For 18 items
        // that is negligible, so we keep the code simple.
        foreach (SourceItemViewModel item in Items)
        {
            item.IsPresent = true;
        }
    }

    private bool CanSelectAll() => Items.Any(item => !item.IsPresent);

    [RelayCommand(CanExecute = nameof(CanClearAll))]
    private void ClearAll()
    {
        foreach (SourceItemViewModel item in Items)
        {
            item.IsPresent = false;
        }
    }

    private bool CanClearAll() => Items.Any(item => item.IsPresent);

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SourceItemViewModel.IsPresent))
        {
            Recalculate();
        }
    }

    /// <summary>
    /// The ViewModel does not compute anything itself: it collects the flags,
    /// asks the model (Identification) and stores the result for the view.
    /// </summary>
    private void Recalculate()
    {
        HashSet<string> presentCodes = Items
            .Where(item => item.IsPresent)
            .Select(item => item.Code)
            .ToHashSet();

        IdentificationResult result = Identification.Calculate(_sources, presentCodes);

        foreach (SourceGroupViewModel group in Groups)
        {
            group.Update(result[group.Category]);
        }

        Total = result.Total;

        // Buttons re-check whether they should be enabled.
        SelectAllCommand.NotifyCanExecuteChanged();
        ClearAllCommand.NotifyCanExecuteChanged();
    }
}
