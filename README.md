# Schedule One Error Analyzer

This repository is the project workspace for the Schedule One error analyzer.

## Layout

- `src/ErrorAnalyzer.Core`: shared analyzer logic
- `src/ErrorAnalyzer.WASM`: browser-targeted .NET WASM build
- `src/ErrorAnalyzer.Plugin`: plugin project
- `src/scheduleone-error-analyzer`: React frontend
- `tests/ErrorAnalyzer.Core.Tests`: .NET tests
- `ErrorAnalyzer.sln`: root .NET workspace for core, WASM, plugin, and tests

## Workspace commands

Install frontend dependencies from the repository root:

```bash
bun install
```

Common commands:

```bash
dotnet restore ErrorAnalyzer.sln
dotnet workload restore src/ErrorAnalyzer.WASM/ErrorAnalyzer.WASM.csproj
bun run dev
bun run build
bun run build:pages s1-error-analyzer
bun run build:pages:site s1-error-analyzer
bun run build:core-docs
bun run lint
dotnet test tests/ErrorAnalyzer.Core.Tests/ErrorAnalyzer.Core.Tests.csproj
bun run test:workspace
```

`bun run dev` and `bun run build` delegate into the React app package and still publish/sync the WASM assets before the frontend build runs.

## Shared metadata

- `Directory.Build.props` is the shared source of truth for common .NET build settings and the repository version.
- `ErrorAnalyzer.Core` generates `ErrorAnalyzerBuildInfo.Version` from that shared version so the plugin and WASM host do not hardcode their own copies.
- `DiagnosisAdvice` in `ErrorAnalyzer.Core` is the shared cross-platform advice contract used by the plugin, React app, and Discord bot.

## NuGet publishing

- `ErrorAnalyzer.Core` is prepared to publish as the `ScheduleOne.ErrorAnalyzer.Core` NuGet package.
- GitHub Actions workflow: `.github/workflows/publish-nuget.yml`
- The current package version is taken from `<ErrorAnalyzerVersion>` in `Directory.Build.props`.
- Publish by either:
  - pushing a version bump to `Directory.Build.props` on a `release/**` or `releases/**` branch, or
  - running the workflow manually after updating `<ErrorAnalyzerVersion>` in `Directory.Build.props`
- Repository secret required: `NUGET_API_KEY`

## GitHub Pages

The GitHub Pages workflow lives at `.github/workflows/deploy-pages.yml` and publishes both the React analyzer app and `ErrorAnalyzer.Core` DocFX documentation in one static site. Use `bun run build:pages:site s1-error-analyzer` to build the combined Pages output locally; the analyzer is served from the site root and the generated core docs are emitted at `docs/` by default. The DocFX site includes authored Markdown guides under `.docfx/docs` alongside the generated API reference. Set `CORE_DOCS_ENDPOINT` to change the endpoint (for example `CORE_DOCS_ENDPOINT=docs/reference bun run build:pages:site s1-error-analyzer`).
