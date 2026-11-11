using System.Text.Json;

namespace UnionTypes.Legacy;

// Both properties can be set simultaneously: the type cannot enforce exclusivity.
public sealed record LegacyDescription(string? Text, LegacyTextObject? Object)
{
    public static LegacyDescription Parse(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Undefined or JsonValueKind.Null => new(null, null),
        JsonValueKind.String => new(value.GetString()!, null),
        JsonValueKind.Object => ParseObject(value),
        _ => throw new JsonException("description must be a string, text object, null or absent.")
    };

    private static LegacyDescription ParseObject(JsonElement value)
    {
        if (!value.TryGetProperty("type", out var type) ||
            type.ValueKind != JsonValueKind.String || type.GetString() != "/type/text" ||
            !value.TryGetProperty("value", out var text) || text.ValueKind != JsonValueKind.String)
            throw new JsonException("description object requires type=/type/text and a string value.");
        return new(null, new(text.GetString()!));
    }

    public string Render()
    {
        if (Text is not null && Object is not null)
            throw new InvalidOperationException("Both description alternatives are set.");
        if (Text is not null) return $"text: {Text}";
        if (Object is not null) return $"object: {Object.Value}";
        return "none";
    }
}

public sealed record LegacyTextObject(string Value);
