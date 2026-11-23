using RiskManager.Core.Calculations;
using RiskManager.Core.Domain;

namespace RiskManager.App.ViewModels;

/// <summary>
/// One row of the ranked risk table. It is only shown, never edited: every
/// recalculation creates new rows, so there are no changes to notify about.
/// </summary>
/// <param name="Rank">The position in the table: 1 is the greatest risk.</param>
public sealed record RankedRisk(int Rank, RiskAssessment Risk, PriorityLevel Priority);
