using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RiskManager.App.ViewModels;

/// <summary>
/// One cell of table 5.1: the probability of a risk event given by one expert.
/// </summary>
public partial class ExpertEstimateViewModel : ObservableValidator
{
    /// <summary>
    /// The estimate in [0; 1]; null means "not entered yet" and is not an error.
    /// An invalid value is stored as well (so that the user sees what was typed),
    /// only HasErrors becomes true.
    /// </summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(0.0, 1.0, ErrorMessage = "Оцінка має бути в межах від 0 до 1")]
    public partial double? Value { get; set; }
}
