using RiskManager.App.ViewModels;
using RiskManager.Core.Domain;

namespace RiskManager.Tests;

public class EstimateRowViewModelTests
{
    private const int Precision = 10;

    private readonly EstimateRowViewModel _row = new(AnalysisTests.Event("t1", RiskCategory.Technical));

    [Theory]
    [InlineData(null)]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void Cell_ValidOrEmptyValue_HasNoErrors(double? value)
    {
        var cell = new ExpertEstimateViewModel { Value = value };

        Assert.False(cell.HasErrors);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.5)]
    [InlineData(double.NaN)]
    public void Cell_InvalidValue_IsStoredAndReported(double value)
    {
        var cell = new ExpertEstimateViewModel { Value = value };

        Assert.True(cell.HasErrors);
        Assert.Equal(value, cell.Value);
        Assert.Single(cell.GetErrors(nameof(ExpertEstimateViewModel.Value)));
    }

    [Fact]
    public void Cell_CorrectedValue_ClearsError()
    {
        var cell = new ExpertEstimateViewModel { Value = 1.5 };

        cell.Value = 0.5;

        Assert.False(cell.HasErrors);
    }

    [Fact]
    public void NewRow_IsEmpty()
    {
        Assert.Equal(EstimateRowViewModel.ExpertCount, _row.Estimates.Count);
        AssertNotAssessed();
    }

    [Fact]
    public void RowWithOneMissingEstimate_IsNotAssessed()
    {
        Fill(0.5, loss: 0.5);
        _row.Estimates[9].Value = null;

        AssertNotAssessed();
    }

    [Fact]
    public void RowWithoutLoss_IsNotAssessed()
    {
        Fill(0.5, loss: null);

        AssertNotAssessed();
    }

    [Fact]
    public void CompleteRow_CalculatesResults()
    {
        // 0.0, 0.1, ..., 0.9 — the average is 0.45.
        for (int i = 0; i < EstimateRowViewModel.ExpertCount; i++)
        {
            _row.Estimates[i].Value = i / 10.0;
        }

        _row.Loss = 0.4;

        Assert.True(_row.IsComplete);
        Assert.Equal(0.45, _row.Probability!.Value, Precision);
        Assert.Equal(0.18, _row.Value!.Value, Precision);
        Assert.Equal(ProbabilityLevel.Medium, _row.ProbabilityLevel);
        Assert.Same(_row.Event, _row.Assessment!.Event);
    }

    [Fact]
    public void CompleteRow_ChangedEstimate_Recalculates()
    {
        Fill(0.5, loss: 0.5);

        _row.Estimates[0].Value = 1.0;

        Assert.Equal(0.55, _row.Probability!.Value, Precision);
        Assert.Equal(0.275, _row.Value!.Value, Precision);
    }

    [Fact]
    public void InvalidEstimate_ResetsAndCorrectionRestoresResults()
    {
        Fill(0.5, loss: 0.5);

        _row.Estimates[3].Value = 1.5;
        AssertNotAssessed();

        _row.Estimates[3].Value = 0.5;
        Assert.Equal(0.25, _row.Value!.Value, Precision);
    }

    [Fact]
    public void InvalidLoss_ResetsAndCorrectionRestoresResults()
    {
        // Also proves that validation runs before the recalculation: otherwise
        // Core would receive 1.5 and throw.
        Fill(0.5, loss: 0.5);

        _row.Loss = 1.5;
        Assert.True(_row.HasErrors);
        AssertNotAssessed();

        _row.Loss = 0.2;
        Assert.False(_row.HasErrors);
        Assert.Equal(0.1, _row.Value!.Value, Precision);
    }

    [Theory]
    [InlineData(nameof(EstimateRowViewModel.Probability))]
    [InlineData(nameof(EstimateRowViewModel.Value))]
    [InlineData(nameof(EstimateRowViewModel.ProbabilityLevel))]
    [InlineData(nameof(EstimateRowViewModel.IsComplete))]
    public void CompletingRow_NotifiesAboutDerivedProperties(string propertyName)
    {
        Fill(0.5, loss: 0.5);
        _row.Estimates[0].Value = null;

        Assert.PropertyChanged(_row, propertyName, () => _row.Estimates[0].Value = 0.5);
    }

    private void Fill(double estimate, double? loss)
    {
        foreach (ExpertEstimateViewModel cell in _row.Estimates)
        {
            cell.Value = estimate;
        }

        _row.Loss = loss;
    }

    private void AssertNotAssessed()
    {
        Assert.False(_row.IsComplete);
        Assert.Null(_row.Assessment);
        Assert.Null(_row.Probability);
        Assert.Null(_row.Value);
        Assert.Null(_row.ProbabilityLevel);
    }
}
