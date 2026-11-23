using RiskManager.Core.Domain;

namespace RiskManager.Core.Calculations;

/// <summary>
/// Stage 2.4: mitigation priority. The range [min; max] of risk values is split
/// into three equal parts of width mpr = (max - min) / 3 (formula 5.11):
/// Low = [min; min + mpr], Medium = (min + mpr; min + 2mpr], High = (min + 2mpr; max].
/// </summary>
public static class Prioritization
{
    /// <summary>
    /// Assigns a priority to every item.
    /// </summary>
    /// <param name="items">Anything that has a risk value: assessments, mitigation results...</param>
    /// <param name="valueSelector">How to get the value from an item, e.g. <c>r =&gt; r.Value</c>.</param>
    /// <remarks>
    /// If all values are equal (max = min), mpr = 0 and every item lands in the first
    /// interval, so all of them get the Low priority — exactly as the formulas say.
    /// </remarks>
    public static PrioritizationResult<T> Prioritize<T>(
        IReadOnlyCollection<T> items,
        Func<T, double> valueSelector)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(valueSelector);

        if (items.Count == 0)
        {
            return new PrioritizationResult<T>([], Min: 0, Max: 0);
        }

        List<(T Item, double Value)> valued = items
            .Select(item => (item, valueSelector(item)))
            .ToList();

        double min = valued.Min(v => v.Value);
        double max = valued.Max(v => v.Value);

        // The result owns the formulas of the bounds, so the bounds shown to the
        // user are always the ones that were applied.
        var bounds = new PrioritizationResult<T>([], min, max);
        double lowUpper = Guard.Normalize(bounds.LowUpper);
        double mediumUpper = Guard.Normalize(bounds.MediumUpper);

        List<PrioritizedItem<T>> prioritized = valued
            .Select(v =>
            {
                double value = Guard.Normalize(v.Value);
                PriorityLevel priority =
                    value <= lowUpper ? PriorityLevel.Low :
                    value <= mediumUpper ? PriorityLevel.Medium :
                    PriorityLevel.High;

                return new PrioritizedItem<T>(v.Item, v.Value, priority);
            })
            .ToList();

        return bounds with { Items = prioritized };
    }
}

/// <summary>An item together with its value and assigned priority.</summary>
public sealed record PrioritizedItem<T>(T Item, double Value, PriorityLevel Priority);

/// <summary>All prioritized items and the interval bounds used.</summary>
public sealed record PrioritizationResult<T>(
    IReadOnlyList<PrioritizedItem<T>> Items,
    double Min,
    double Max)
{
    /// <summary>mpr — width of one priority interval.</summary>
    public double Step => (Max - Min) / 3;

    /// <summary>min + mpr — the upper bound of the Low interval.</summary>
    public double LowUpper => Min + Step;

    /// <summary>min + 2mpr — the upper bound of the Medium interval.</summary>
    public double MediumUpper => Min + 2 * Step;

    /// <summary>Set HER — first-order risks.</summary>
    public IReadOnlyList<PrioritizedItem<T>> High => ByPriority(PriorityLevel.High);

    /// <summary>Set MER — second-order risks.</summary>
    public IReadOnlyList<PrioritizedItem<T>> Medium => ByPriority(PriorityLevel.Medium);

    /// <summary>Set LER — third-order risks.</summary>
    public IReadOnlyList<PrioritizedItem<T>> Low => ByPriority(PriorityLevel.Low);

    private List<PrioritizedItem<T>> ByPriority(PriorityLevel priority) =>
        Items.Where(i => i.Priority == priority).ToList();
}
