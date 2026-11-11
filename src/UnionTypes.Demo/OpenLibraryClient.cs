using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace UnionTypes.Demo;

public sealed record WorkFound(WorkPayload Work);
public sealed record WorkNotFound(string WorkId);
public sealed record WorkRedirect(string Location);
public sealed record HttpFailure(HttpStatusCode Status);
public sealed record NetworkFailure(string Message);
public sealed record InvalidPayload(string Message);
public sealed record RequestTimedOut;
public union FetchResult(WorkFound, WorkNotFound, WorkRedirect, HttpFailure, NetworkFailure, InvalidPayload, RequestTimedOut);

public sealed class OpenLibraryClient(HttpClient http)
{
    public async Task<FetchResult> FetchAsync(string workId, CancellationToken cancellationToken = default)
    {
        if (!Regex.IsMatch(workId, @"\AOL[0-9]+W\z", RegexOptions.CultureInvariant))
            throw new ArgumentException("Expected an Open Library work ID, e.g. OL45883W.", nameof(workId));
        try
        {
            using var response = await http.GetAsync(
                $"https://openlibrary.org/works/{workId}.json", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound) return new WorkNotFound(workId);
            if (!response.IsSuccessStatusCode) return new HttpFailure(response.StatusCode);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("type", out var type) &&
                type.ValueKind == JsonValueKind.Object && type.TryGetProperty("key", out var key) &&
                key.ValueKind == JsonValueKind.String && key.GetString() == "/type/redirect")
            {
                if (!root.TryGetProperty("location", out var location) || location.ValueKind != JsonValueKind.String ||
                    !Regex.IsMatch(location.GetString()!, @"\A/works/OL[0-9]+W\z", RegexOptions.CultureInvariant))
                    throw new JsonException("Redirect requires a work location.");
                return new WorkRedirect(location.GetString()!);
            }
            var work = WorkPayload.Parse(json);
            _ = Descriptions.FromJson(work.Description);
            return new WorkFound(work);
        }
        catch (JsonException exception) { return new InvalidPayload(exception.Message); }
        catch (HttpRequestException exception) { return new NetworkFailure(exception.Message); }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { return new RequestTimedOut(); }
        // Caller cancellation propagates instead of being reported as an HTTP failure.
    }
}
