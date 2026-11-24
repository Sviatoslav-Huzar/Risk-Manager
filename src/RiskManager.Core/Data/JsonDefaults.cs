using System.Text.Json;
using System.Text.Json.Serialization;

namespace RiskManager.Core.Data;

/// <summary>JSON settings shared by the catalog and the project files.</summary>
internal static class JsonDefaults
{
    public static JsonSerializerOptions Strict { get; } = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        NumberHandling = JsonNumberHandling.Strict,
        // "Technical" in JSON <-> RiskCategory.Technical in C#.
        Converters = { new JsonStringEnumConverter() },
        // Fail loudly instead of silently producing half-filled objects:
        RespectRequiredConstructorParameters = true,              // a missing "code" is an error
        RespectNullableAnnotations = true,                        // "code": null is an error
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, // a typo like "descripton" is an error
    };
}
