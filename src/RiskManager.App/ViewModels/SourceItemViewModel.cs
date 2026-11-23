using CommunityToolkit.Mvvm.ComponentModel;
using RiskManager.Core.Domain;

namespace RiskManager.App.ViewModels;

/// <summary>
/// One risk source on the "Risk sources" tab — a single check box.
/// Wraps the immutable <see cref="RiskSource"/> from the catalog and adds
/// the project-specific state: whether this source applies to the project.
/// </summary>
public partial class SourceItemViewModel : ObservableObject
{
    public SourceItemViewModel(RiskSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        Source = source;
    }

    public RiskSource Source { get; }

    public string Code => Source.Code;

    public string Description => Source.Description;

    public RiskCategory Category => Source.Category;

    /// <summary>
    /// The flag of the methodology: true means 1 (the SRS has this risk source),
    /// false means 0. The source generator implements the setter so that it raises
    /// PropertyChanged, which is what keeps the check box and the totals in sync.
    /// </summary>
    [ObservableProperty]
    public partial bool IsPresent { get; set; }
}
