using RiskManager.Core.Domain;

namespace RiskManager.Core.Calculations;

/// <summary>
/// Stage 1 of the model: identification of risk sources (1.1) and risk events (1.2).
/// </summary>
/// <remarks>
/// Formulas (5.2)-(5.4): for every category the share is
/// (number of items of this category flagged as 1) / (number of all items),
/// e.g. tc = (t1 + ... + t7) / 18. The total R is the sum of the four shares.
/// The denominators are taken from the catalog, not hard-coded.
/// </remarks>
public static class Identification
{
    /// <param name="items">All catalog items of one kind (all sources or all events).</param>
    /// <param name="presentCodes">Codes of the items flagged as 1 for the current project.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="items"/> is empty or <paramref name="presentCodes"/> contains an unknown code.
    /// </exception>
    public static IdentificationResult Calculate<T>(
        IReadOnlyCollection<T> items,
        IReadOnlySet<string> presentCodes)
        where T : ICategorizedItem
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(presentCodes);

        if (items.Count == 0)
        {
            throw new ArgumentException("At least one catalog item is required.", nameof(items));
        }

        List<string> unknownCodes = presentCodes
            .Except(items.Select(i => i.Code))
            .ToList();
        if (unknownCodes.Count > 0)
        {
            throw new ArgumentException(
                $"Unknown codes: {string.Join(", ", unknownCodes)}.", nameof(presentCodes));
        }

        int totalCount = items.Count;

        List<CategoryShare> categories = Enum.GetValues<RiskCategory>()
            .Select(category =>
            {
                List<T> inCategory = items.Where(i => i.Category == category).ToList();
                int presentCount = inCategory.Count(i => presentCodes.Contains(i.Code));

                // (double) is essential: 7 / 18 in integer arithmetic is 0.
                double share = (double)presentCount / totalCount;

                return new CategoryShare(category, presentCount, inCategory.Count, share);
            })
            .ToList();

        return new IdentificationResult(categories, totalCount);
    }
}

/// <summary>Result of stage 1 for one category, e.g. tc for technical risks.</summary>
/// <param name="PresentCount">How many items of the category are flagged as 1.</param>
/// <param name="CategoryCount">How many items the category has in the catalog.</param>
/// <param name="Share">PresentCount / total number of items (value in [0; 1]).</param>
public sealed record CategoryShare(
    RiskCategory Category,
    int PresentCount,
    int CategoryCount,
    double Share)
{
    /// <summary>"Risks of this category take place": at least one item is flagged.</summary>
    public bool IsPresent => PresentCount > 0;
}

/// <summary>Result of stage 1: shares of all four categories and their sum R.</summary>
public sealed record IdentificationResult(IReadOnlyList<CategoryShare> Categories, int TotalCount)
{
    /// <summary>R = tc + cc + pc + mc (formula 5.3).</summary>
    public double Total => Categories.Sum(c => c.Share);

    /// <summary>Lets you write result[RiskCategory.Cost] instead of searching the list.</summary>
    public CategoryShare this[RiskCategory category] =>
        Categories.Single(c => c.Category == category);
}
