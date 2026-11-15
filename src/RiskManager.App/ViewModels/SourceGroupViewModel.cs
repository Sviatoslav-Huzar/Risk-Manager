using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using RiskManager.Core.Calculations;
using RiskManager.Core.Domain;

namespace RiskManager.App.ViewModels;

/// <summary>
/// One category block on the "Risk sources" tab, e.g. "Technical — 2 of 7 (11.11 %)",
/// together with its check boxes.
/// </summary>
public partial class SourceGroupViewModel : ObservableObject
{
    public SourceGroupViewModel(RiskCategory category, IReadOnlyList<SourceItemViewModel> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        Category = category;
        Items = items;
    }

    public RiskCategory Category { get; }

    /// <summary>
    /// The sources of this category. The list itself never changes (only the
    /// items' IsPresent flags do), so a plain read-only list is enough —
    /// no ObservableCollection is needed.
    /// </summary>
    public IReadOnlyList<SourceItemViewModel> Items { get; }

    public int CategoryCount => Items.Count;

    /// <summary>How many sources of the category are flagged.</summary>
    [ObservableProperty]
    public partial int PresentCount { get; private set; }

    /// <summary>The category share of formula (5.2), e.g. tc.</summary>
    [ObservableProperty]
    public partial double Share { get; private set; }

    /// <summary>Called by <see cref="SourcesViewModel"/> after every recalculation.</summary>
    internal void Update(CategoryShare result)
    {
        // A developer-only sanity check: it is removed from Release builds.
        Debug.Assert(result.Category == Category, "Result belongs to another category.");

        PresentCount = result.PresentCount;
        Share = result.Share;
    }
}
