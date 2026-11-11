using System.Net;
using UnionTypes.Demo;
using Xunit;

namespace UnionTypes.Tests;

public sealed class HttpAndCommandTests
{
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => send(request, token);
    }

    private static HttpClient Client(HttpStatusCode status, string body = "") =>
        new(new Handler((request, _) =>
        {
            Assert.Equal("https://openlibrary.org/works/OL45883W.json", request.RequestUri!.AbsoluteUri);
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
        }));

    [Fact]
    public async Task SuccessReturnsValidatedPayload()
    {
        using var http = Client(HttpStatusCode.OK, """{"title":"T","description":"hello"}""");
        var result = await new OpenLibraryClient(http).FetchAsync("OL45883W");
        Assert.Equal("T", Assert.IsType<WorkFound>(result.Value).Work.Title);
    }

    [Theory]
    [InlineData(404, typeof(WorkNotFound))]
    [InlineData(429, typeof(HttpFailure))]
    [InlineData(500, typeof(HttpFailure))]
    [InlineData(204, typeof(InvalidPayload))]
    [InlineData(200, typeof(InvalidPayload))]
    public async Task StatusAndMalformedBodyBecomeExplicitCases(int status, Type expected)
    {
        using var http = Client((HttpStatusCode)status, "not JSON");
        var result = await new OpenLibraryClient(http).FetchAsync("OL45883W");
        Assert.IsType(expected, result.Value);
        if (result.Value is HttpFailure failure) Assert.Equal(status, (int)failure.Status);
    }

    [Fact]
    public async Task NetworkFailureIsExplicit()
    {
        using var http = new HttpClient(new Handler((_, _) => throw new HttpRequestException("offline")));
        var result = await new OpenLibraryClient(http).FetchAsync("OL45883W");
        Assert.Equal("offline", Assert.IsType<NetworkFailure>(result.Value).Message);
    }

    [Fact]
    public async Task ValidJsonWithUnsupportedDescriptionIsInvalidPayload()
    {
        using var http = Client(HttpStatusCode.OK, """{"title":"T","description":42}""");
        var result = await new OpenLibraryClient(http).FetchAsync("OL45883W");
        Assert.IsType<InvalidPayload>(result.Value);
    }

    [Theory]
    [InlineData("OL45883W", "/works/OL45804W")]
    [InlineData("OL14933414W", "/works/OL27513W")]
    [InlineData("OL262758W", "/works/OL27482W")]
    public async Task OriginalRedirectsAreExplicitResults(string fixture, string target)
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "samples", "responses", fixture + ".json"));
        using var http = Client(HttpStatusCode.OK, json);
        var result = await new OpenLibraryClient(http).FetchAsync("OL45883W");
        Assert.Equal(target, Assert.IsType<WorkRedirect>(result.Value).Location);
        using var output = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(0, await DemoApplication.RunAsync(["union", "--live"], output, error, http));
        Assert.Contains(target, output.ToString());
    }

    [Theory]
    [InlineData("{\"type\":{\"key\":\"/type/redirect\"}}")]
    [InlineData("{\"type\":{\"key\":\"/type/redirect\"},\"location\":\"https://example.com\"}")]
    public async Task InvalidRedirectIsRejected(string json)
    {
        using var http = Client(HttpStatusCode.OK, json);
        var result = await new OpenLibraryClient(http).FetchAsync("OL45883W");
        Assert.IsType<InvalidPayload>(result.Value);
    }

    [Fact]
    public async Task TimeoutIsExplicit()
    {
        using var http = new HttpClient(new Handler((_, _) => throw new TaskCanceledException("timeout")));
        var result = await new OpenLibraryClient(http).FetchAsync("OL45883W");
        Assert.IsType<RequestTimedOut>(result.Value);
    }

    [Fact]
    public async Task CallerCancellationPropagates()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var http = new HttpClient(new Handler((_, token) => Task.FromCanceled<HttpResponseMessage>(token)));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new OpenLibraryClient(http).FetchAsync("OL45883W", cancellation.Token));
    }

    [Theory]
    [InlineData("../bad")]
    [InlineData("OL45883W\n")]
    public async Task InvalidWorkIdNeverSendsRequest(string workId)
    {
        using var http = new HttpClient(new Handler((_, _) => throw new InvalidOperationException("Must not send")));
        await Assert.ThrowsAsync<ArgumentException>(() => new OpenLibraryClient(http).FetchAsync(workId));
    }

    [Theory]
    [InlineData("legacy")]
    [InlineData("union")]
    [InlineData("compare")]
    public async Task OfflineModesDisplayEveryFixture(string mode)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(0, await DemoApplication.RunAsync([mode], output, error));
        Assert.Empty(error.ToString());
        foreach (var kind in new[] { "String", "Object", "Undefined", "Null" })
            Assert.Contains($"[{kind}]", output.ToString());
        if (mode is "legacy" or "compare") Assert.Contains("legacy: text: A sample description.", output.ToString());
        if (mode is "union" or "compare") Assert.Contains("union: object: A sample description.", output.ToString());
    }

    [Theory]
    [InlineData("legacy")]
    [InlineData("union")]
    [InlineData("compare")]
    public async Task LiveModesUseInjectedHttpClient(string mode)
    {
        using var http = Client(HttpStatusCode.OK, """{"title":"From HTTP","description":null}""");
        using var output = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(0, await DemoApplication.RunAsync([mode, "--live"], output, error, http));
        Assert.Contains("From HTTP [Null]", output.ToString());
        Assert.Empty(error.ToString());
    }

    [Fact]
    public async Task LiveFailureReturnsNonzeroExitCode()
    {
        using var http = Client(HttpStatusCode.NotFound);
        using var output = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(1, await DemoApplication.RunAsync(["union", "--live"], output, error, http));
        Assert.Contains("Not found", error.ToString());
    }

    [Fact]
    public async Task ExamplesAndUsageAreRunnable()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(0, await DemoApplication.RunAsync(["examples"], output, error));
        Assert.Contains("boxed type: System.Int32", output.ToString());
        Assert.Contains("default union: null", output.ToString());
        Assert.Equal(2, await DemoApplication.RunAsync(["unknown"], output, error));
        Assert.Contains("Usage:", error.ToString());
    }
}
