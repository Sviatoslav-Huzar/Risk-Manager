using CommunityToolkit.Mvvm.ComponentModel;
using RiskManager.Core.Domain;

namespace RiskManager.App.ViewModels;

/// <summary>
/// One check box of the measure list of one risk: whether the measure can
/// reduce or eliminate this risk (stage 3.1).
/// </summary>
public partial class MeasureItemViewModel : ObservableObject
{
    public MeasureItemViewModel(Measure measure)
    {
        ArgumentNullException.ThrowIfNull(measure);
        Measure = measure;
    }

    public Measure Measure { get; }

    public string Code => Measure.Code;

    public string Description => Measure.Description;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
