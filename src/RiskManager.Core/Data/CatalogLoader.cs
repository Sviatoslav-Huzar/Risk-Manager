using System.Text.Json;
using RiskManager.Core.Domain;

namespace RiskManager.Core.Data;

/// <summary>
/// Reads the reference catalog (risk sources, risk events, measures) from JSON.
/// </summary>
public static class CatalogLoader
{
    /// <summary>
    /// Name of the catalog.json file embedded into RiskManager.Core.dll
    /// (set explicitly via LogicalName in the .csproj).
    /// </summary>
    public const string DefaultResourceName = "RiskManager.Core.Data.catalog.json";

    /// <summary>Loads the catalog that ships inside the assembly.</summary>
    public static RiskCatalog LoadDefault()
    {
        using Stream stream =
            typeof(CatalogLoader).Assembly.GetManifestResourceStream(DefaultResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{DefaultResourceName}' was not found.");

        return Load(stream);
    }

    /// <summary>Loads and validates a catalog from any JSON stream.</summary>
    /// <exception cref="InvalidDataException">The JSON is malformed or the data is inconsistent.</exception>
    public static RiskCatalog Load(Stream json)
    {
        ArgumentNullException.ThrowIfNull(json);

        RiskCatalog? catalog;
        try
        {
            catalog = JsonSerializer.Deserialize<RiskCatalog>(json, JsonDefaults.Strict);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Catalog JSON is invalid: {ex.Message}", ex);
        }

        if (catalog is null)
        {
            throw new InvalidDataException("Catalog JSON is empty.");
        }

        Validate(catalog);
        return catalog;
    }

    private static void Validate(RiskCatalog catalog)
    {
        ValidateItems(catalog.Sources, "sources");
        ValidateItems(catalog.Events, "events");
        ValidateItems(catalog.Measures, "measures");
    }

    private static void ValidateItems(IReadOnlyList<ICatalogItem> items, string listName)
    {
        if (items.Count == 0)
        {
            throw new InvalidDataException($"Catalog list '{listName}' is empty.");
        }

        ICatalogItem? blank = items.FirstOrDefault(
            i => string.IsNullOrWhiteSpace(i.Code) || string.IsNullOrWhiteSpace(i.Description));
        if (blank is not null)
        {
            throw new InvalidDataException(
                $"Catalog list '{listName}' contains an item with an empty code or description: {blank}.");
        }

        List<string> duplicates = items
            .GroupBy(i => i.Code, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicates.Count > 0)
        {
            throw new InvalidDataException(
                $"Catalog list '{listName}' has duplicate codes: {string.Join(", ", duplicates)}.");
        }
    }
}
