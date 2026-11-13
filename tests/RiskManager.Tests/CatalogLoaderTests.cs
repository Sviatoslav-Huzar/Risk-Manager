using System.Text;
using RiskManager.Core.Data;
using RiskManager.Core.Domain;

namespace RiskManager.Tests;

public class CatalogLoaderTests
{
    private readonly RiskCatalog _catalog = CatalogLoader.LoadDefault();

    [Fact]
    public void LoadDefault_ContainsAllItemsFromMethodology()
    {
        Assert.Equal(18, _catalog.Sources.Count);
        Assert.Equal(46, _catalog.Events.Count);
        Assert.Equal(19, _catalog.Measures.Count);
    }

    [Theory]
    [InlineData(RiskCategory.Technical, 7)]
    [InlineData(RiskCategory.Cost, 3)]
    [InlineData(RiskCategory.Schedule, 3)]
    [InlineData(RiskCategory.Management, 5)]
    public void LoadDefault_SourcesPerCategory(RiskCategory category, int expected)
    {
        Assert.Equal(expected, _catalog.Sources.Count(s => s.Category == category));
    }

    [Theory]
    [InlineData(RiskCategory.Technical, 11)]
    [InlineData(RiskCategory.Cost, 8)]
    [InlineData(RiskCategory.Schedule, 11)]
    [InlineData(RiskCategory.Management, 16)]
    public void LoadDefault_EventsPerCategory(RiskCategory category, int expected)
    {
        Assert.Equal(expected, _catalog.Events.Count(e => e.Category == category));
    }

    [Fact]
    public void LoadDefault_ReadsTextsAndEnumsCorrectly()
    {
        RiskEvent first = _catalog.Events[0];

        Assert.Equal("t1", first.Code);
        Assert.Equal(RiskCategory.Technical, first.Category);
        Assert.StartsWith("Затримки у постачанні обладнання", first.Description);
    }

    [Fact]
    public void Load_DuplicateCodes_Throws()
    {
        const string json = """
            {
              "sources":  [ { "code": "t1", "category": "Technical", "description": "A" },
                            { "code": "t1", "category": "Technical", "description": "B" } ],
              "events":   [ { "code": "t1", "category": "Technical", "description": "E" } ],
              "measures": [ { "code": "ev1", "description": "M" } ]
            }
            """;

        var ex = Assert.Throws<InvalidDataException>(() => CatalogLoader.Load(ToStream(json)));
        Assert.Contains("duplicate", ex.Message);
    }

    [Fact]
    public void Load_MisspelledProperty_Throws()
    {
        const string json = """
            {
              "sources":  [ { "code": "t1", "category": "Technical", "descripton": "typo" } ],
              "events":   [ { "code": "t1", "category": "Technical", "description": "E" } ],
              "measures": [ { "code": "ev1", "description": "M" } ]
            }
            """;

        Assert.Throws<InvalidDataException>(() => CatalogLoader.Load(ToStream(json)));
    }

    [Fact]
    public void Load_EmptyList_Throws()
    {
        const string json = """
            { "sources": [], "events": [], "measures": [] }
            """;

        Assert.Throws<InvalidDataException>(() => CatalogLoader.Load(ToStream(json)));
    }

    private static MemoryStream ToStream(string text) => new(Encoding.UTF8.GetBytes(text));
}
