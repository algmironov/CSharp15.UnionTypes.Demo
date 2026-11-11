# C# 15 Union Types Demo

A small .NET 11 console example comparing nullable legacy properties with a native C# 15 union for heterogeneous Open Library descriptions. No third-party union library is used.

Requires release SDK 11.0.100 (compatible patches allowed). Run from the repository root:

```sh
dotnet build CSharp15.UnionTypes.Demo.slnx -c Release
dotnet test CSharp15.UnionTypes.Demo.slnx -c Release --no-build
dotnet run --project src/UnionTypes.Demo -c Release --no-build -- compare
dotnet run --project src/UnionTypes.Demo -c Release --no-build -- examples
dotnet run --project src/UnionTypes.Demo -c Release --no-build -- union --live OL82563W
```

Modes: `legacy`, `union`, `compare`; optional `--live [work-id]`. Default execution reads local synthetic fixtures without network access. Initial package restore may require internet. Real unmodified API responses have separate provenance metadata in `samples/responses/`.

The transport model preserves missing versus explicit null and unknown JSON fields. Domain mapping deliberately treats missing/null as `NoDescription`. Invalid description shapes throw `JsonException`; HTTP errors and JSON redirect records have explicit union cases. The default live ID is a redirect; the application prints its target without following it automatically. Caller cancellation propagates. Generated unions can still contain null via `default`; value-type cases are boxed.

See [Russian README](README.md), [architecture](docs/architecture.md), and [actual validation results](docs/test-results.md). A distribution license has not been selected by the owner.
