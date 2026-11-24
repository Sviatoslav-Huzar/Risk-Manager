using System.IO;
using System.Text;
using RiskManager.Core.Data;
using RiskManager.Core.Domain;

namespace RiskManager.Tests;

public sealed class ProjectStoreTests : IDisposable
{
    private static readonly RiskCatalog Catalog = CatalogLoader.LoadDefault();

    private static readonly double?[] TenEstimates = [0.1, null, 0.3, 1.5, 0, 1, 0.123456789, null, 0.9, 0.5];

    private static readonly RiskProject Sample = new(
        RiskProject.CurrentVersion,
        ["t1", "m3"],
        ["t1", "c2"],
        [
            new RiskData("t1", TenEstimates, 0.4, ["ev1", "ev7"], [new EvaluationData("ev1", 0.2, null)]),
            new RiskData("m16", new double?[10], null, ["ev19"], []),
        ]);

    private readonly string _directory = Directory.CreateTempSubdirectory("RiskManagerTests").FullName;

    private string FilePath => Path.Combine(_directory, "project.riskproj");

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact]
    public async Task SavedProject_IsLoadedUnchanged()
    {
        await ProjectStore.SaveAsync(FilePath, Sample);

        RiskProject loaded = await ProjectStore.LoadAsync(FilePath, Catalog);

        Assert.Equal(ProjectStore.Serialize(Sample), ProjectStore.Serialize(loaded));
        Assert.Equal(TenEstimates, loaded.Risks[0].Estimates);
    }

    [Fact]
    public async Task EmptyProject_CanBeSavedAndLoaded()
    {
        await ProjectStore.SaveAsync(FilePath, RiskProject.Empty);

        RiskProject loaded = await ProjectStore.LoadAsync(FilePath, Catalog);

        Assert.Empty(loaded.Risks);
    }

    [Fact]
    public async Task Save_ReplacesFileAndLeavesNoTemporaryFile()
    {
        await ProjectStore.SaveAsync(FilePath, RiskProject.Empty);
        await ProjectStore.SaveAsync(FilePath, Sample);

        Assert.Equal(2, (await ProjectStore.LoadAsync(FilePath, Catalog)).Risks.Count);
        Assert.Equal([FilePath], Directory.GetFiles(_directory));
    }

    [Fact]
    public async Task FailedSave_KeepsPreviousFile()
    {
        await ProjectStore.SaveAsync(FilePath, Sample);
        RiskProject unserializable = Sample with
        {
            Risks = [new RiskData("t1", new double?[10], double.NaN, [], [])],
        };

        await Assert.ThrowsAnyAsync<ArgumentException>(() => ProjectStore.SaveAsync(FilePath, unserializable));

        Assert.Equal(2, (await ProjectStore.LoadAsync(FilePath, Catalog)).Risks.Count);
        Assert.Equal([FilePath], Directory.GetFiles(_directory));
    }

    [Fact]
    public async Task MissingFile_Throws()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(() => ProjectStore.LoadAsync(FilePath, Catalog));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("""{ "version": "1", "sources": [], "events": [], "risks": [] }""")]
    [InlineData("""{ "version": 1, "sources": [], "events": [] }""")]
    [InlineData("""{ "version": 1, "sources": null, "events": [], "risks": [] }""")]
    [InlineData("""{ "version": 1, "sources": [], "events": [], "risks": [], "extra": 1 }""")]
    [InlineData("""{ "version": 1, "sources": [null], "events": [], "risks": [] }""")]
    [InlineData("""{ "version": 1, "sources": ["x1"], "events": [], "risks": [] }""")]
    [InlineData("""{ "version": 1, "sources": [], "events": ["t1", "t1"], "risks": [] }""")]
    [InlineData("""{ "version": 1, "sources": [], "events": [], "risks": [null] }""")]
    public async Task InvalidFile_Throws(string json)
    {
        await Assert.ThrowsAsync<InvalidDataException>(() => Load(json));
    }

    [Theory]
    [InlineData("x1", "0.5", "[]", "[]")]                                                       // unknown event
    [InlineData("t1", "1e999", "[]", "[]")]                                                     // infinity
    [InlineData("t1", "\"0.5\"", "[]", "[]")]                                                   // text instead of a number
    [InlineData("t1", "0.5", "[\"ev99\"]", "[]")]                                               // unknown measure
    [InlineData("t1", "0.5", "[]", "[null]")]
    [InlineData("t1", "0.5", "[]", """[{ "measure": "ev99", "probability": 0, "loss": 0 }]""")]
    public async Task InvalidRisk_Throws(string code, string loss, string measures, string evaluations)
    {
        await Assert.ThrowsAsync<InvalidDataException>(() => Load(RiskJson(code, 10, loss, measures, evaluations)));
    }

    [Theory]
    [InlineData(9, false)]
    [InlineData(10, true)]
    [InlineData(11, false)]
    public async Task Estimates_MustBeTen(int count, bool isValid)
    {
        string json = RiskJson("t1", count, "0.5", "[]", "[]");

        if (isValid)
        {
            Assert.Single((await Load(json)).Risks);
        }
        else
        {
            await Assert.ThrowsAsync<InvalidDataException>(() => Load(json));
        }
    }

    [Fact]
    public async Task NewerVersion_IsNamedInMessage()
    {
        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => Load("""{ "version": 2, "somethingNew": true }"""));

        Assert.Contains("Version 2", exception.Message);
    }

    private static Task<RiskProject> Load(string json) =>
        ProjectStore.LoadAsync(new MemoryStream(Encoding.UTF8.GetBytes(json)), Catalog);

    private static string RiskJson(string code, int estimateCount, string loss, string measures, string evaluations) =>
        $$"""
        {
          "version": 1, "sources": [], "events": [],
          "risks": [{
            "event": "{{code}}",
            "estimates": [{{string.Join(", ", Enumerable.Repeat("0.5", estimateCount))}}],
            "loss": {{loss}},
            "measures": {{measures}},
            "evaluations": {{evaluations}}
          }]
        }
        """;
}
