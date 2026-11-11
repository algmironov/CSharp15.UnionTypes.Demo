using System.Text.Json;
using System.Text.Json.Serialization;

namespace UnionTypes.Demo;

// JsonElement preserves the wire shape, including Undefined versus Null.
// STJ already supports this transport shape; no union converter is necessary.
public sealed class WorkPayload
{
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public JsonElement Description { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }

    public static WorkPayload Parse(string json)
    {
        var result = JsonSerializer.Deserialize<WorkPayload>(json)
            ?? throw new JsonException("Work must be a JSON object.");
        if (result.Title is null) throw new JsonException("title must be a string.");
        return result;
    }
}
