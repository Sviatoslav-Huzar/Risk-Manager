using RiskManager.App.ViewModels;
using RiskManager.Core.Data;
using RiskManager.Core.Domain;

namespace RiskManager.Tests;

public class MonitoringViewModelTests
{
    private const int Precision = 10;
    private const int Seed = 42;

    private readonly RiskCatalog _catalog = CatalogLoader.LoadDefault();
    private readonly ChecklistViewModel _events;
    private readonly EstimatesViewModel _estimates;
    private readonly MeasuresViewModel _measures;
    private readonly MonitoringViewModel _vm;

    public MonitoringViewModelTests()
    {
        _events = new ChecklistViewModel(_catalog.Events);
        _estimates = new EstimatesViewModel(_events);
        _measures = new MeasuresViewModel(new AnalysisViewModel(_estimates), _catalog.Measures);
        _vm = new MonitoringViewModel(_measures, new Random(Seed));
    }

    [Fact]
    public void NothingPlanned_EverythingIsEmptyOrZero()
    {
        Assert.Empty(_vm.Rows);
        Assert.Null(_vm.SelectedRow);
        Assert.Equal(0, _vm.CompleteCount);
        Assert.Equal(0, _vm.MissingCount);
        Assert.Equal(0, _vm.TotalBefore);
        Assert.Equal(0, _vm.TotalReductionRatio);
        Assert.False(_vm.FillRandomCommand.CanExecute(null));
    }

    [Fact]
    public void Rows_AreOnlyRisksWithMeasures()
    {
        Assess("t1", loss: 0.2);
        Assess("c1", loss: 0.6);

        Mark("t1", "ev1");

        Assert.Equal("t1", Assert.Single(_vm.Rows).Ranked.Risk.Event.Code);
        Assert.Same(Row("t1"), _vm.SelectedRow);
        Assert.Equal(2, _vm.MissingCount);
    }

    [Fact]
    public void ExistingPlans_GetRowsInConstructor()
    {
        Assess("t1", loss: 0.2);
        Mark("t1", "ev1");

        var vm = new MonitoringViewModel(_measures);

        Assert.Single(vm.Rows);
    }

    [Theory]
    [InlineData(null, 0.5)]
    [InlineData(0.5, null)]
    [InlineData(1.5, 0.5)]
    [InlineData(0.5, -0.1)]
    public void IncompleteOrInvalidEvaluation_HasNoResult(double? probability, double? loss)
    {
        Assess("t1", loss: 0.2);
        Mark("t1", "ev1");

        Evaluate("t1", "ev1", probability, loss);

        Assert.Null(Evaluation("t1", "ev1").Value);
        Assert.False(Row("t1").IsComplete);
        Assert.Null(Row("t1").PriorityAfter);
        Assert.Equal(0, _vm.CompleteCount);
        Assert.Equal(1, _vm.MissingCount);
    }

    [Theory]
    [InlineData(-0.1, true)]
    [InlineData(1.1, true)]
    [InlineData(0.0, false)]
    [InlineData(1.0, false)]
    public void Evaluation_ValidatesBothNumbers(double value, bool hasErrors)
    {
        var evaluation = new MeasureEvaluationViewModel(_catalog.Measures[0]) { Probability = value };
        Assert.Equal(hasErrors, evaluation.HasErrors);

        evaluation = new MeasureEvaluationViewModel(_catalog.Measures[0]) { Loss = value };
        Assert.Equal(hasErrors, evaluation.HasErrors);
    }

    [Fact]
    public void CompleteEvaluation_CalculatesResult()
    {
        Assess("t1", loss: 0.8); // vr = 0.5 * 0.8 = 0.4
        Mark("t1", "ev1");

        Evaluate("t1", "ev1", 0.2, 0.5); // evr = 0.1

        RiskMonitoringViewModel row = Row("t1");
        Assert.Equal(0.1, Evaluation("t1", "ev1").Value!.Value, Precision);
        Assert.Equal(0.1, row.ValueAfter!.Value, Precision);
        Assert.Equal(0.75, row.ReductionRatio!.Value, Precision);
        Assert.Equal("ev1", row.BestCode);
        Assert.True(Evaluation("t1", "ev1").IsBest);
        Assert.Equal(0, _vm.MissingCount);
    }

    [Fact]
    public void SeveralCandidates_BestHasSmallestValue()
    {
        Assess("t1", loss: 0.8);
        Mark("t1", "ev1", "ev2", "ev3");

        Evaluate("t1", "ev1", 0.5, 0.5);
        Evaluate("t1", "ev2", 0.2, 0.2);
        Assert.False(Row("t1").IsComplete); // ev3 is not evaluated yet
        Assert.False(Evaluation("t1", "ev2").IsBest);

        Evaluate("t1", "ev3", 0.3, 0.3);

        Assert.Equal("ev2", Row("t1").BestCode);
        Assert.Equal(["ev2"], Row("t1").Evaluations.Where(e => e.IsBest).Select(e => e.Code));
    }

    [Fact]
    public void ChangedNumber_ChangesBest()
    {
        Assess("t1", loss: 0.8);
        Mark("t1", "ev1", "ev2");
        Evaluate("t1", "ev1", 0.5, 0.5);
        Evaluate("t1", "ev2", 0.2, 0.2);

        Evaluation("t1", "ev1").Loss = 0;

        Assert.Equal("ev1", Row("t1").BestCode);
        Assert.False(Evaluation("t1", "ev2").IsBest);
    }

    [Fact]
    public void HarmfulMeasure_HasNegativeReduction()
    {
        Assess("t1", loss: 0.2); // vr = 0.1
        Mark("t1", "ev1");

        Evaluate("t1", "ev1", 0.5, 0.4); // evr = 0.2

        Assert.Equal(-1, Row("t1").ReductionRatio!.Value, Precision);
    }

    [Fact]
    public void UnmarkedMeasure_DisappearsAndReturnsWithNumbers()
    {
        Assess("t1", loss: 0.8);
        Mark("t1", "ev1", "ev2");
        Evaluate("t1", "ev1", 0.1, 0.1);
        Evaluate("t1", "ev2", 0.5, 0.5);

        Unmark("t1", "ev1");
        Assert.Equal("ev2", Row("t1").BestCode);

        Mark("t1", "ev1");
        Assert.Equal("ev1", Row("t1").BestCode);
        Assert.Equal(["ev1", "ev2"], Row("t1").Evaluations.Select(e => e.Code));
    }

    [Fact]
    public void RemovedRisk_ReturnsWithNumbersAndSelection()
    {
        Assess("t1", loss: 0.2);
        Assess("c1", loss: 0.8);
        Mark("t1", "ev1");
        Mark("c1", "ev1");
        Evaluate("t1", "ev1", 0.1, 0.1);
        _vm.SelectedRow = Row("t1");

        Event("t1").IsPresent = false;
        Assert.Same(Row("c1"), Assert.Single(_vm.Rows));
        Assert.Same(Row("c1"), _vm.SelectedRow);
        Assert.Equal(0, _vm.CompleteCount);

        Event("t1").IsPresent = true;
        Assert.True(Row("t1").IsComplete);
        Assert.Same(Row("t1"), _vm.SelectedRow);
        Assert.Equal(1, _vm.CompleteCount);
    }

    [Fact]
    public void ChangedEstimate_UpdatesReductionAndKeepsTable()
    {
        Assess("t1", loss: 0.8);
        Mark("t1", "ev1");
        Evaluate("t1", "ev1", 0.2, 0.5); // evr = 0.1
        IReadOnlyList<MeasureEvaluationViewModel> evaluations = Row("t1").Evaluations;

        _estimates.Rows[0].Loss = 0.4; // vr = 0.2

        Assert.Equal(0.5, Row("t1").ReductionRatio!.Value, Precision);
        Assert.Equal(0.2, _vm.TotalBefore, Precision);
        Assert.Same(evaluations, Row("t1").Evaluations);
    }

    [Fact]
    public void Priorities_AreSetAgainFromNewValues()
    {
        // Before: t1 is the greatest. After: evr = 0.01, 0.5, 0.9, m1 is the greatest.
        Assess("t1", loss: 0.9);
        Assess("c1", loss: 0.5);
        Assess("m1", loss: 0.1);
        Mark("t1", "ev1");
        Mark("c1", "ev1");
        Mark("m1", "ev1");
        Assert.Equal(PriorityLevel.High, Row("t1").Ranked.Priority);

        Evaluate("t1", "ev1", 0.1, 0.1);
        Evaluate("c1", "ev1", 1.0, 0.5);
        Evaluate("m1", "ev1", 1.0, 0.9);

        Assert.Equal(PriorityLevel.Low, Row("t1").PriorityAfter);
        Assert.Equal(PriorityLevel.Medium, Row("c1").PriorityAfter);
        Assert.Equal(PriorityLevel.High, Row("m1").PriorityAfter);
        Assert.Equal(0.01, _vm.Results.Min, Precision);
        Assert.Equal(0.9, _vm.Results.Max, Precision);
        Assert.Equal(PriorityLevel.High, Row("t1").Ranked.Priority); // "before" is untouched
    }

    [Fact]
    public void Totals_IncludeOnlyEvaluatedRisks()
    {
        Assess("t1", loss: 0.8); // vr = 0.4
        Assess("c1", loss: 0.2); // vr = 0.1, not evaluated
        Mark("t1", "ev1");
        Mark("c1", "ev1");

        Evaluate("t1", "ev1", 0.2, 0.5); // evr = 0.1

        Assert.Equal(0.4, _vm.TotalBefore, Precision);
        Assert.Equal(0.1, _vm.TotalAfter, Precision);
        Assert.Equal(0.75, _vm.TotalReductionRatio, Precision);
        Assert.Equal(1, _vm.MissingCount);
    }

    [Theory]
    [InlineData(nameof(MonitoringViewModel.Results))]
    [InlineData(nameof(MonitoringViewModel.CompleteCount))]
    [InlineData(nameof(MonitoringViewModel.MissingCount))]
    [InlineData(nameof(MonitoringViewModel.TotalBefore))]
    [InlineData(nameof(MonitoringViewModel.TotalAfter))]
    [InlineData(nameof(MonitoringViewModel.TotalReductionRatio))]
    public void CompletingRisk_NotifiesAboutSummary(string propertyName)
    {
        Assess("t1", loss: 0.8);
        Mark("t1", "ev1");

        Assert.PropertyChanged(_vm, propertyName, () => Evaluate("t1", "ev1", 0.2, 0.5));
    }

    [Theory]
    [InlineData(nameof(RiskMonitoringViewModel.Result))]
    [InlineData(nameof(RiskMonitoringViewModel.IsComplete))]
    [InlineData(nameof(RiskMonitoringViewModel.ValueAfter))]
    [InlineData(nameof(RiskMonitoringViewModel.ReductionRatio))]
    [InlineData(nameof(RiskMonitoringViewModel.BestCode))]
    [InlineData(nameof(RiskMonitoringViewModel.PriorityAfter))]
    public void CompletingRisk_NotifiesAboutRow(string propertyName)
    {
        Assess("t1", loss: 0.8);
        Mark("t1", "ev1");

        Assert.PropertyChanged(Row("t1"), propertyName, () => Evaluate("t1", "ev1", 0.2, 0.5));
    }

    [Fact]
    public void FillRandom_FillsEmptyCellsAndReducesEveryRisk()
    {
        Assess("t1", loss: 0.8);
        Assess("c1", loss: 0.4);
        Mark("t1", "ev1", "ev2");
        Mark("c1", "ev3");
        Evaluation("t1", "ev1").Probability = 0.123;
        Assert.True(_vm.FillRandomCommand.CanExecute(null));

        _vm.FillRandomCommand.Execute(null);

        Assert.Equal(0.123, Evaluation("t1", "ev1").Probability);
        Assert.Equal(2, _vm.CompleteCount);
        Assert.All(_vm.Rows, row => Assert.InRange(row.ReductionRatio!.Value, 0.0, 1.0));
        Assert.False(_vm.FillRandomCommand.CanExecute(null));
    }

    [Fact]
    public void FillRandom_BecomesEnabledWhenRiskGetsMeasure()
    {
        bool raised = false;
        _vm.FillRandomCommand.CanExecuteChanged += (_, _) => raised = true;

        Assess("t1", loss: 0.8);
        Mark("t1", "ev1");

        Assert.True(raised);
        Assert.True(_vm.FillRandomCommand.CanExecute(null));
    }

    [Fact]
    public void ViewThatResetsSelection_DoesNotLoseIt()
    {
        Assess("t1", loss: 0.2);
        Assess("c1", loss: 0.8);
        Mark("t1", "ev1");
        Mark("c1", "ev1");
        RiskMonitoringViewModel selected = Row("t1");
        _vm.SelectedRow = selected;
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MonitoringViewModel.Rows))
            {
                _vm.SelectedRow = null;
            }
        };

        Mark("c1", "ev2");

        Assert.Same(selected, _vm.SelectedRow);
    }

    [Fact]
    public void NullMeasures_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new MonitoringViewModel(null!));
    }

    private void Assess(string code, double loss)
    {
        Event(code).IsPresent = true;
        EstimateRowViewModel row = _estimates.Rows.Single(r => r.Code == code);

        foreach (ExpertEstimateViewModel cell in row.Estimates)
        {
            cell.Value = 0.5;
        }

        row.Loss = loss;
    }

    private void Mark(string code, params string[] measureCodes)
    {
        foreach (string measureCode in measureCodes)
        {
            MeasureItem(code, measureCode).IsSelected = true;
        }
    }

    private void Unmark(string code, string measureCode) =>
        MeasureItem(code, measureCode).IsSelected = false;

    private MeasureItemViewModel MeasureItem(string code, string measureCode) =>
        _measures.Rows.Single(row => row.Ranked.Risk.Event.Code == code)
            .Items.Single(item => item.Code == measureCode);

    private void Evaluate(string code, string measureCode, double? probability, double? loss)
    {
        MeasureEvaluationViewModel evaluation = Evaluation(code, measureCode);
        evaluation.Probability = probability;
        evaluation.Loss = loss;
    }

    private MeasureEvaluationViewModel Evaluation(string code, string measureCode) =>
        Row(code).Evaluations.Single(evaluation => evaluation.Code == measureCode);

    private RiskMonitoringViewModel Row(string code) =>
        _vm.Rows.Single(row => row.Ranked.Risk.Event.Code == code);

    private ChecklistItemViewModel Event(string code) => _events.Items.Single(item => item.Code == code);
}
