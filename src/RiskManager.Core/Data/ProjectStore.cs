using System.Text.Json;
using RiskManager.Core.Calculations;
using RiskManager.Core.Domain;

namespace RiskManager.Core.Data;

/// <summary>
/// Saves projects to JSON files and loads them back.
/// </summary>
public static class ProjectStore
{
    /// <summary>
    /// Writes the project to a temporary file first and then replaces the target,
    /// so a failure in the middle of writing cannot destroy the previous version.
    /// </summary>
    public static async Task SaveAsync(string path, RiskProject project, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(project);

        string temporaryPath = path + ".tmp";

        try
        {
            await using (FileStream stream = File.Create(temporaryPath))
            {
                await JsonSerializer.SerializeAsync(stream, project, JsonDefaults.Strict, cancellationToken);
            }

            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            // After a successful move there is nothing to delete.
            File.Delete(temporaryPath);
        }
    }

    /// <summary>Loads a project and checks it against the catalog.</summary>
    /// <exception cref="InvalidDataException">The file is not a valid project.</exception>
    public static async Task<RiskProject> LoadAsync(
        string path,
        RiskCatalog catalog,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        await using FileStream stream = File.OpenRead(path);
        return await LoadAsync(stream, catalog, cancellationToken);
    }

    /// <inheritdoc cref="LoadAsync(string, RiskCatalog, CancellationToken)"/>
    public static async Task<RiskProject> LoadAsync(
        Stream json,
        RiskCatalog catalog,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(catalog);

        RiskProject project;
        try
        {
            using JsonDocument document = await JsonDocument.ParseAsync(json, cancellationToken: cancellationToken);

            // Checked before anything else: a newer format may have other fields,
            // and "unsupported version" explains more than "unknown field".
            CheckVersion(document.RootElement);

            project = document.Deserialize<RiskProject>(JsonDefaults.Strict)
                ?? throw new InvalidDataException("The project file is empty.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"The project file is invalid: {ex.Message}", ex);
        }

        Validate(project, catalog);
        return project;
    }

    /// <summary>The text of a project file; equal projects have equal texts.</summary>
    public static string Serialize(RiskProject project) =>
        JsonSerializer.Serialize(project, JsonDefaults.Strict);

    private static void CheckVersion(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("version", out JsonElement element)
            || element.ValueKind != JsonValueKind.Number
            || !element.TryGetInt32(out int version))
        {
            throw new InvalidDataException("The file is not a risk project: it has no version.");
        }

        if (version != RiskProject.CurrentVersion)
        {
            throw new InvalidDataException(
                $"Version {version} of the project file is not supported (expected {RiskProject.CurrentVersion}).");
        }
    }

    // The range [0; 1] is not checked: the user may save a project with a
    // mistyped number, and it must look the same after opening.
    private static void Validate(RiskProject project, RiskCatalog catalog)
    {
        ValidateCodes(project.Sources, catalog.Sources, "sources");
        ValidateCodes(project.Events, catalog.Events, "events");

        if (project.Risks.Any(risk => risk is null))
        {
            throw new InvalidDataException("List 'risks' contains an empty element.");
        }

        ValidateCodes(project.Risks.Select(risk => risk.Event), catalog.Events, "risks");

        foreach (RiskData risk in project.Risks)
        {
            if (risk.Estimates.Count != Analysis.ExpertCount)
            {
                throw new InvalidDataException(
                    $"Risk '{risk.Event}' has {risk.Estimates.Count} estimates instead of {Analysis.ExpertCount}.");
            }

            if (risk.Evaluations.Any(evaluation => evaluation is null))
            {
                throw new InvalidDataException($"Evaluations of risk '{risk.Event}' contain an empty element.");
            }

            ValidateCodes(risk.Measures, catalog.Measures, $"measures of {risk.Event}");
            ValidateCodes(
                risk.Evaluations.Select(evaluation => evaluation.Measure),
                catalog.Measures,
                $"evaluations of {risk.Event}");

            IEnumerable<double?> numbers = risk.Estimates
                .Append(risk.Loss)
                .Concat(risk.Evaluations.SelectMany(e => new[] { e.Probability, e.Loss }));

            if (numbers.Any(number => number is double value && !double.IsFinite(value)))
            {
                throw new InvalidDataException($"Risk '{risk.Event}' contains a number that is too large.");
            }
        }
    }

    private static void ValidateCodes(
        IEnumerable<string> codes,
        IEnumerable<ICatalogItem> catalogItems,
        string listName)
    {
        HashSet<string> known = catalogItems.Select(item => item.Code).ToHashSet();
        HashSet<string> used = [];

        foreach (string code in codes)
        {
            if (!known.Contains(code))
            {
                throw new InvalidDataException($"List '{listName}' contains an unknown code '{code}'.");
            }

            if (!used.Add(code))
            {
                throw new InvalidDataException($"List '{listName}' contains code '{code}' twice.");
            }
        }
    }
}
