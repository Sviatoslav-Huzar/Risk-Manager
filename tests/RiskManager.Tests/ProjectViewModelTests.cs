using RiskManager.App.ViewModels;
using RiskManager.Core.Data;

namespace RiskManager.Tests;

public class ProjectViewModelTests
{
    [Fact]
    public void Constructor_CreatesSourcesTabFromCatalog()
    {
        var vm = new ProjectViewModel(CatalogLoader.LoadDefault());

        Assert.NotNull(vm.Sources);
        Assert.Equal(18, vm.Sources.Items.Count);
    }

    [Fact]
    public void Constructor_CreatesEventsTabFromCatalog()
    {
        var vm = new ProjectViewModel(CatalogLoader.LoadDefault());

        Assert.Equal(46, vm.Events.Items.Count);
        Assert.Equal([11, 8, 11, 16], vm.Events.Groups.Select(g => g.CategoryCount));
    }

    [Fact]
    public void SourcesAndEvents_AreIndependent()
    {
        // Sources and events share codes (both have "t1"), so this makes sure
        // the two tabs do not accidentally share state.
        var vm = new ProjectViewModel(CatalogLoader.LoadDefault());

        vm.Sources.Items.Single(item => item.Code == "t1").IsPresent = true;

        Assert.Equal(1.0 / 18, vm.Sources.Total, 10);
        Assert.Equal(0, vm.Events.Total);
        Assert.All(vm.Events.Items, item => Assert.False(item.IsPresent));
    }

    [Fact]
    public void Estimates_FollowEventsTab()
    {
        var vm = new ProjectViewModel(CatalogLoader.LoadDefault());

        vm.Events.Items.Single(item => item.Code == "t1").IsPresent = true;

        Assert.Equal("t1", Assert.Single(vm.Estimates.Rows).Code);
    }

    [Fact]
    public void Analysis_FollowsEstimatesTab()
    {
        var vm = new ProjectViewModel(CatalogLoader.LoadDefault());
        vm.Events.Items.Single(item => item.Code == "t1").IsPresent = true;

        vm.Estimates.FillRandomCommand.Execute(null);

        Assert.Equal("t1", Assert.Single(vm.Analysis.Risks).Risk.Event.Code);
    }

    [Fact]
    public void Measures_FollowAnalysisTab()
    {
        var vm = new ProjectViewModel(CatalogLoader.LoadDefault());
        vm.Events.Items.Single(item => item.Code == "t1").IsPresent = true;

        vm.Estimates.FillRandomCommand.Execute(null);

        RiskMeasuresViewModel row = Assert.Single(vm.Measures.Rows);
        Assert.Equal("t1", row.Ranked.Risk.Event.Code);
        Assert.Equal(19, row.Items.Count);
    }

    [Fact]
    public void Monitoring_FollowsMeasuresTab()
    {
        var vm = new ProjectViewModel(CatalogLoader.LoadDefault());
        vm.Events.Items.Single(item => item.Code == "t1").IsPresent = true;
        vm.Estimates.FillRandomCommand.Execute(null);

        vm.Measures.Rows[0].Items[4].IsSelected = true;

        RiskMonitoringViewModel row = Assert.Single(vm.Monitoring.Rows);
        Assert.Equal("ev5", Assert.Single(row.Evaluations).Code);
    }

    [Fact]
    public void NewProject_IsEmpty()
    {
        var vm = new ProjectViewModel(CatalogLoader.LoadDefault());

        Assert.Equal(ProjectStore.Serialize(RiskProject.Empty), ProjectStore.Serialize(vm.ToProject()));
    }

    [Fact]
    public void RestoredProject_LooksLikeOriginal()
    {
        ProjectViewModel original = CreateFilledProject();

        var restored = new ProjectViewModel(CatalogLoader.LoadDefault(), original.ToProject());

        AssertSameData(original, restored);
        Assert.Equal(original.Sources.Total, restored.Sources.Total);
        Assert.Equal(original.Events.Total, restored.Events.Total);
        Assert.Equal(original.Estimates.CompleteCount, restored.Estimates.CompleteCount);
        Assert.Equal(original.Analysis.Risks, restored.Analysis.Risks);
        Assert.Equal(
            original.Measures.Rows.Select(row => row.SelectedCodes),
            restored.Measures.Rows.Select(row => row.SelectedCodes));
        Assert.Equal(
            original.Monitoring.Rows.Select(row => row.BestCode),
            restored.Monitoring.Rows.Select(row => row.BestCode));
        Assert.Equal(original.Monitoring.TotalAfter, restored.Monitoring.TotalAfter);
        Assert.Equal(original.Charts.ValuesAfter, restored.Charts.ValuesAfter);
        Assert.True(restored.Monitoring.CompleteCount > 0);
    }

    [Fact]
    public void RestoredProject_StillReactsToChanges()
    {
        var restored = new ProjectViewModel(CatalogLoader.LoadDefault(), CreateFilledProject().ToProject());
        int risks = restored.Analysis.Risks.Count;

        restored.Estimates.Rows[0].Loss = null;

        Assert.Equal(risks - 1, restored.Analysis.Risks.Count);
        Assert.Equal(risks - 1, restored.Measures.Rows.Count);
    }

    [Fact]
    public void HiddenData_SurvivesSaving()
    {
        // All events are unchecked: no rows on any tab, but the numbers remain.
        ProjectViewModel original = CreateFilledProject();
        original.Events.ClearAllCommand.Execute(null);

        var restored = new ProjectViewModel(CatalogLoader.LoadDefault(), original.ToProject());
        AssertSameData(original, restored); // also while nothing has been shown yet

        foreach (string code in new[] { "t1", "c2", "m16" })
        {
            Event(original, code).IsPresent = true;
            Event(restored, code).IsPresent = true;
        }

        Assert.Equal(3, restored.Monitoring.CompleteCount);
        AssertSameData(original, restored);
        Assert.Equal(original.Monitoring.TotalAfter, restored.Monitoring.TotalAfter);
    }

    [Fact]
    public void EvaluationOfUnmarkedMeasure_SurvivesSaving()
    {
        ProjectViewModel original = CreateFilledProject();
        RiskMeasuresViewModel row = original.Measures.Rows[0];
        double? probability = original.Monitoring.Rows[0].Evaluations[0].Probability;
        row.Items[0].IsSelected = false;

        var restored = new ProjectViewModel(CatalogLoader.LoadDefault(), original.ToProject());
        AssertSameData(original, restored);

        restored.Measures.Rows[0].Items[0].IsSelected = true;
        Assert.Equal(probability, restored.Monitoring.Rows[0].Evaluations[0].Probability);
    }

    [Fact]
    public void ClearedNumbers_DoNotReturnFromFile()
    {
        var restored = new ProjectViewModel(CatalogLoader.LoadDefault(), CreateFilledProject().ToProject());
        MeasureEvaluationViewModel evaluation = restored.Monitoring.Rows[0].Evaluations[0];
        evaluation.Probability = null;
        evaluation.Loss = null;
        restored.Measures.Rows[1].Items[0].IsSelected = false;

        var again = new ProjectViewModel(CatalogLoader.LoadDefault(), restored.ToProject());

        AssertSameData(restored, again);
        Assert.Null(again.Monitoring.Rows[0].Evaluations[0].Probability);
        Assert.False(again.Measures.Rows[1].Items[0].IsSelected);
    }

    [Fact]
    public void InvalidNumber_IsRestoredWithItsError()
    {
        ProjectViewModel original = CreateFilledProject();
        original.Estimates.Rows[0].Loss = 1.5;

        var restored = new ProjectViewModel(CatalogLoader.LoadDefault(), original.ToProject());

        Assert.Equal(1.5, restored.Estimates.Rows[0].Loss);
        Assert.True(restored.Estimates.Rows[0].HasErrors);
        AssertSameData(original, restored);
    }

    [Fact]
    public void OrderOfMarking_DoesNotChangeFile()
    {
        ProjectViewModel first = CreateFilledProject();
        var second = new ProjectViewModel(CatalogLoader.LoadDefault(), first.ToProject());

        foreach (int index in new[] { 8, 3 })
        {
            Mark(first, index);
        }

        foreach (int index in new[] { 3, 8 })
        {
            Mark(second, index);
        }

        AssertSameData(first, second);

        static void Mark(ProjectViewModel project, int index)
        {
            project.Measures.Rows[0].Items[index].IsSelected = true;
            string code = project.Measures.Rows[0].Items[index].Code;
            project.Monitoring.Rows[0].Evaluations.Single(e => e.Code == code).Loss = 0.5;
        }
    }

    [Fact]
    public void Constructor_NullCatalog_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ProjectViewModel(null!));
    }

    private static ProjectViewModel CreateFilledProject()
    {
        var vm = new ProjectViewModel(CatalogLoader.LoadDefault());
        vm.Sources.Items[2].IsPresent = true;

        foreach (string code in new[] { "m16", "t1", "c2" })
        {
            Event(vm, code).IsPresent = true;
        }

        vm.Estimates.FillRandomCommand.Execute(null);

        foreach (RiskMeasuresViewModel row in vm.Measures.Rows)
        {
            row.Items[0].IsSelected = true;
            row.Items[6].IsSelected = true;
        }

        vm.Monitoring.FillRandomCommand.Execute(null);
        return vm;
    }

    private static ChecklistItemViewModel Event(ProjectViewModel project, string code) =>
        project.Events.Items.Single(item => item.Code == code);

    private static void AssertSameData(ProjectViewModel expected, ProjectViewModel actual) =>
        Assert.Equal(ProjectStore.Serialize(expected.ToProject()), ProjectStore.Serialize(actual.ToProject()));
}
