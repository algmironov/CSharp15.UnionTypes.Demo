using System.Text.Json;

namespace UnionTypes.Demo;

public sealed record PlainText(string Value);
public sealed record TextObject(string Value);
public sealed record NoDescription;
public union Description(PlainText, TextObject, NoDescription);

public static class Descriptions
{
    public static Description FromJson(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Undefined or JsonValueKind.Null => new NoDescription(),
        JsonValueKind.String => new PlainText(value.GetString()!),
        JsonValueKind.Object => ReadObject(value),
        _ => throw new JsonException("description must be a string, text object, null or absent.")
    };

    private static Description ReadObject(JsonElement value)
    {
        if (!value.TryGetProperty("type", out var type) ||
            type.ValueKind != JsonValueKind.String || type.GetString() != "/type/text" ||
            !value.TryGetProperty("value", out var text) || text.ValueKind != JsonValueKind.String)
            throw new JsonException("description object requires type=/type/text and a string value.");
        return new TextObject(text.GetString()!);
    }

    // No discard: adding a case requires revisiting this switch.
    public static string Render(Description description) => description switch
    {
        PlainText text => $"text: {text.Value}",
        TextObject text => $"object: {text.Value}",
        NoDescription => "none",
        null => throw new InvalidOperationException("Uninitialized Description union.")
    };
}
