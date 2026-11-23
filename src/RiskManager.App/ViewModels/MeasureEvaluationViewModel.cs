using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using RiskManager.Core.Calculations;
using RiskManager.Core.Domain;

namespace RiskManager.App.ViewModels;

/// <summary>
/// One candidate measure of one risk: the expected probability epr and loss elr
/// after the measure is applied (stage 4.1, formulas 5.14-5.16).
/// </summary>
public partial class MeasureEvaluationViewModel : ObservableValidator
{
    public MeasureEvaluationViewModel(Measure measure)
    {
        ArgumentNullException.ThrowIfNull(measure);
        Measure = measure;
    }

    public Measure Measure { get; }

    public string Code => Measure.Code;

    public string Description => Measure.Description;

    /// <summary>epr in [0; 1]; null means "not entered yet".</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(0.0, 1.0, ErrorMessage = "Ймовірність має бути в межах від 0 до 1")]
    public partial double? Probability { get; set; }

    /// <summary>elr in [0; 1]; null means "not entered yet".</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(0.0, 1.0, ErrorMessage = "Збитки мають бути в межах від 0 до 1")]
    public partial double? Loss { get; set; }

    /// <summary>The evaluated measure; null while a number is missing or invalid.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsComplete))]
    [NotifyPropertyChangedFor(nameof(Value))]
    public partial MeasureOption? Option { get; private set; }

    public bool IsComplete => Option is not null;

    /// <summary>evr = epr * elr (formula 5.16).</summary>
    public double? Value => Option?.Value;

    /// <summary>Set by <see cref="RiskMonitoringViewModel"/>, which compares the candidates.</summary>
    [ObservableProperty]
    public partial bool IsBest { get; internal set; }

    /// <summary>
    /// Fills only the empty cells. A simulated measure leaves 30-90 % of the
    /// probability and of the loss, so it always helps, but not equally well.
    /// </summary>
    internal void FillEmpty(RiskAssessment risk, Random random)
    {
        double probability = Reduce(risk.Probability);
        double loss = Reduce(risk.Loss);

        Probability ??= probability;
        Loss ??= loss;

        double Reduce(double value) => Math.Round(value * (0.3 + 0.6 * random.NextDouble()), 3);
    }

    partial void OnProbabilityChanged(double? value) => Recalculate();

    partial void OnLossChanged(double? value) => Recalculate();

    private void Recalculate()
    {
        // Core throws on values outside [0; 1], so errors are checked first.
        Option = !HasErrors && Probability is double probability && Loss is double loss
            ? new MeasureOption(Measure, probability, loss)
            : null;
    }
}
