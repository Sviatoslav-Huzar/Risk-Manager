using RiskManager.Core.Calculations;
using RiskManager.Core.Data;
using RiskManager.Core.Domain;

namespace RiskManager.Tests;

public class IdentificationTests
{
    private const int Precision = 10;

    private readonly RiskCatalog _catalog = CatalogLoader.LoadDefault();

    [Fact]
    public void Sources_AllFlagged_GivesSharesFromMethodology()
    {
        IdentificationResult result = Identification.Calculate(
            _catalog.Sources, AllCodes(_catalog.Sources));

        // Methodology, stage 1.1: tc = 38.89 %, cc = pc = 16.67 %, mc = 27.78 %.
        Assert.Equal(7.0 / 18, result[RiskCategory.Technical].Share, Precision);
        Assert.Equal(3.0 / 18, result[RiskCategory.Cost].Share, Precision);
        Assert.Equal(3.0 / 18, result[RiskCategory.Schedule].Share, Precision);
        Assert.Equal(5.0 / 18, result[RiskCategory.Management].Share, Precision);
        Assert.Equal(1.0, result.Total, Precision);
    }

    [Fact]
    public void Events_AllFlagged_UsesCatalogSizeAsDenominator()
    {
        IdentificationResult result = Identification.Calculate(
            _catalog.Events, AllCodes(_catalog.Events));

        // Formula (5.4) divides by 46 (the methodology's 25.58 % is 11/43, a known typo).
        Assert.Equal(46, result.TotalCount);
        Assert.Equal(11.0 / 46, result[RiskCategory.Technical].Share, Precision);
        Assert.Equal(8.0 / 46, result[RiskCategory.Cost].Share, Precision);
        Assert.Equal(11.0 / 46, result[RiskCategory.Schedule].Share, Precision);
        Assert.Equal(16.0 / 46, result[RiskCategory.Management].Share, Precision);
    }

    [Fact]
    public void NothingFlagged_AllSharesAreZero()
    {
        IdentificationResult result = Identification.Calculate(
            _catalog.Sources, new HashSet<string>());

        Assert.All(result.Categories, c =>
        {
            Assert.Equal(0, c.Share);
            Assert.False(c.IsPresent);
        });
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public void SomeFlagged_CountsOnlyFlaggedItems()
    {
        var flagged = new HashSet<string> { "t1", "t3", "c2" };

        IdentificationResult result = Identification.Calculate(_catalog.Sources, flagged);

        CategoryShare technical = result[RiskCategory.Technical];
        Assert.Equal(2, technical.PresentCount);
        Assert.Equal(7, technical.CategoryCount);
        Assert.Equal(2.0 / 18, technical.Share, Precision);
        Assert.True(technical.IsPresent);

        Assert.Equal(1.0 / 18, result[RiskCategory.Cost].Share, Precision);
        Assert.False(result[RiskCategory.Schedule].IsPresent);
        Assert.Equal(3.0 / 18, result.Total, Precision);
    }

    [Fact]
    public void WorksWithAnyCatalog_NotOnlyTheDefaultOne()
    {
        // The formulas take the denominator from the data, so a tiny
        // hand-made list must work as well as the full catalog.
        var items = new List<RiskSource>
        {
            new("a", RiskCategory.Technical, "A"),
            new("b", RiskCategory.Technical, "B"),
            new("c", RiskCategory.Cost, "C"),
            new("d", RiskCategory.Management, "D"),
        };

        IdentificationResult result = Identification.Calculate(items, new HashSet<string> { "a", "d" });

        Assert.Equal(0.25, result[RiskCategory.Technical].Share, Precision);
        Assert.Equal(0.25, result[RiskCategory.Management].Share, Precision);
        Assert.Equal(0, result[RiskCategory.Schedule].CategoryCount);
        Assert.Equal(0.5, result.Total, Precision);
    }

    [Fact]
    public void UnknownCode_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Identification.Calculate(_catalog.Sources, new HashSet<string> { "t1", "x99" }));

        Assert.Contains("x99", ex.Message);
    }

    [Fact]
    public void EmptyCatalog_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Identification.Calculate(new List<RiskSource>(), new HashSet<string>()));
    }

    private static HashSet<string> AllCodes(IEnumerable<ICatalogItem> items) =>
        items.Select(i => i.Code).ToHashSet();
}
