using System.Text.Json;
using UnionTypes.Demo;
using UnionTypes.Legacy;
using Xunit;

namespace UnionTypes.Tests;

public sealed class DescriptionTests
{
    [Theory]
    [InlineData("string", JsonValueKind.String, "text: A sample description.")]
    [InlineData("object", JsonValueKind.Object, "object: A sample description.")]
    [InlineData("missing", JsonValueKind.Undefined, "none")]
    [InlineData("null", JsonValueKind.Null, "none")]
    public void FixturesRoundTripAndBothApproachesAgree(string fixture, JsonValueKind kind, string expected)
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "samples", "synthetic", fixture + ".json"));
        var work = WorkPayload.Parse(json);
        Assert.Equal(kind, work.Description.ValueKind);
        Assert.Equal(expected, Descriptions.Render(Descriptions.FromJson(work.Description)));
        Assert.Equal(expected, LegacyDescription.Parse(work.Description).Render());
        var restored = WorkPayload.Parse(JsonSerializer.Serialize(work));
        Assert.Equal(work.Title, restored.Title);
        Assert.Equal(kind, restored.Description.ValueKind);
        Assert.Equal(expected, Descriptions.Render(Descriptions.FromJson(restored.Description)));
    }

    [Theory]
    [InlineData("42")]
    [InlineData("true")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("{\"type\":\"/type/text\"}")]
    [InlineData("{\"type\":\"unknown\",\"value\":\"text\"}")]
    [InlineData("{\"type\":\"/type/text\",\"value\":null}")]
    [InlineData("{\"type\":\"/type/text\",\"value\":123}")]
    public void InvalidDescriptionIsRejectedByBothApproaches(string description)
    {
        using var document = JsonDocument.Parse(description);
        Assert.Throws<JsonException>(() => Descriptions.FromJson(document.RootElement));
        Assert.Throws<JsonException>(() => LegacyDescription.Parse(document.RootElement));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{")]
    [InlineData("{}")]
    [InlineData("{\"title\":null}")]
    [InlineData("{\"title\":42}")]
    public void InvalidEnvelopeIsRejected(string json) => Assert.Throws<JsonException>(() => WorkPayload.Parse(json));

    [Fact]
    public void RoundTripPreservesUnknownFieldsAndObjectMetadata()
    {
        const string json = """{"title":"T","key":"/works/OL1W","description":{"type":"/type/text","value":"","language":"en"}}""";
        using var restored = JsonDocument.Parse(JsonSerializer.Serialize(WorkPayload.Parse(json)));
        Assert.Equal("/works/OL1W", restored.RootElement.GetProperty("key").GetString());
        Assert.Equal("en", restored.RootElement.GetProperty("description").GetProperty("language").GetString());
        Assert.Equal("object: ", Descriptions.Render(Descriptions.FromJson(restored.RootElement.GetProperty("description"))));
    }

    [Fact]
    public void LegacyTypeAllowsInvalidStateButRendererRejectsIt() =>
        Assert.Throws<InvalidOperationException>(() => new LegacyDescription("one", new("two")).Render());

    [Fact]
    public void DefaultDomainUnionIsNotBusinessAbsence() =>
        Assert.Throws<InvalidOperationException>(() => Descriptions.Render(default));

    [Fact]
    public void ImplicitConversionBoxesAnInt()
    {
        NumberOrText value = 42;
        Assert.Equal(42, Assert.IsType<int>(value.Value));
        NumberOrText empty = default;
        Assert.Null(empty.Value);
    }

    [Theory]
    [InlineData("OL45804W", JsonValueKind.String)]
    [InlineData("OL82563W", JsonValueKind.Object)]
    [InlineData("OL27513W", JsonValueKind.String)]
    public void OriginalResponsesAreSupported(string id, JsonValueKind kind)
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "samples", "responses", id + ".json"));
        var work = WorkPayload.Parse(json);
        Assert.Equal(kind, work.Description.ValueKind);
        Assert.Equal(LegacyDescription.Parse(work.Description).Render(), Descriptions.Render(Descriptions.FromJson(work.Description)));
        var restored = WorkPayload.Parse(JsonSerializer.Serialize(work));
        Assert.Equal(kind, restored.Description.ValueKind);
        Assert.Equal(Descriptions.Render(Descriptions.FromJson(work.Description)), Descriptions.Render(Descriptions.FromJson(restored.Description)));
    }
}
