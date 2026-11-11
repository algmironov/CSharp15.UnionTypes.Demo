using System.Text.Json;
using UnionTypes.Demo;
using UnionTypes.Legacy;

return await DemoApplication.RunAsync(args, Console.Out, Console.Error);

public static class DemoApplication
{
    public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error,
        HttpClient? http = null)
    {
        if (args is ["examples"])
        {
            LanguageExamples.Run(output);
            return 0;
        }
        if (args.Length == 0 || args[0] is not ("legacy" or "union" or "compare") ||
            (args.Length != 1 && !(args.Length is 2 or 3 && args[1] == "--live")))
        {
            error.WriteLine("Usage: UnionTypes.Demo <legacy|union|compare> [--live [OL45883W]] | examples");
            return 2;
        }
        try
        {
            if (args.Length > 1)
            {
                using var ownedHttp = http is null ? new HttpClient { Timeout = TimeSpan.FromSeconds(20) } : null;
                var client = http ?? ownedHttp!;
                var result = await new OpenLibraryClient(client).FetchAsync(args.Length == 3 ? args[2] : "OL45883W");
                return result switch
                {
                    WorkFound found => Show(found.Work, args[0], output),
                    WorkNotFound missing => Fail(error, $"Not found: {missing.WorkId}"),
                    WorkRedirect redirect => ShowRedirect(output, redirect.Location),
                    HttpFailure failure => Fail(error, $"HTTP: {(int)failure.Status}"),
                    NetworkFailure failure => Fail(error, $"Network: {failure.Message}"),
                    InvalidPayload failure => Fail(error, $"Invalid JSON: {failure.Message}"),
                    RequestTimedOut => Fail(error, "Request timed out."),
                    null => throw new InvalidOperationException("Uninitialized FetchResult.")
                };
            }
            foreach (var name in new[] { "string", "object", "missing", "null" })
            {
                var path = Path.Combine(AppContext.BaseDirectory, "samples", "synthetic", name + ".json");
                var work = WorkPayload.Parse(await File.ReadAllTextAsync(path));
                Show(work, args[0], output);
            }
            return 0;
        }
        catch (Exception exception) when (exception is JsonException or IOException or ArgumentException)
        { return Fail(error, exception.Message); }
    }

    private static int Show(WorkPayload work, string mode, TextWriter output)
    {
        output.WriteLine($"{work.Title} [{work.Description.ValueKind}]");
        if (mode is "legacy" or "compare") output.WriteLine($"legacy: {LegacyDescription.Parse(work.Description).Render()}");
        if (mode is "union" or "compare") output.WriteLine($"union: {Descriptions.Render(Descriptions.FromJson(work.Description))}");
        return 0;
    }

    private static int Fail(TextWriter error, string message)
    {
        error.WriteLine(message);
        return 1;
    }

    private static int ShowRedirect(TextWriter output, string location)
    {
        output.WriteLine($"Redirect: {location} (repeat --live with the target work ID)");
        return 0;
    }
}
