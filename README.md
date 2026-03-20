# Schedule One Error Analyzer

This repository is the project workspace for the Schedule One error analyzer.

## Layout

- `src/ErrorAnalyzer.Core`: shared analyzer logic
- `src/ErrorAnalyzer.WASM`: browser-targeted .NET WASM build
- `src/ErrorAnalyzer.Plugin`: plugin project
- `src/scheduleone-error-analyzer`: React frontend
- `tests/ErrorAnalyzer.Core.Tests`: .NET tests

## Workspace commands

Install frontend dependencies from the repository root:

```bash
bun install
```

Common commands:

```bash
bun run dev
bun run build
bun run build:pages s1-error-analyzer
bun run lint
dotnet test
```

`bun run dev` and `bun run build` delegate into the React app package and still publish/sync the WASM assets before the frontend build runs.

## NuGet publishing

- `ErrorAnalyzer.Core` is prepared to publish as the `ScheduleOne.ErrorAnalyzer.Core` NuGet package.
- GitHub Actions workflow: `.github/workflows/publish-nuget.yml`
- Publish by either:
  - pushing a version bump to `src/ErrorAnalyzer.Core/ErrorAnalyzer.Core.csproj` on a `release/**` or `releases/**` branch, or
  - running the workflow manually after updating `<Version>` in the project file
- Repository secret required: `NUGET_API_KEY`

## GitHub Pages

The GitHub Pages workflow lives at `.github/workflows/deploy-pages.yml` and builds the frontend from the workspace root before uploading `src/scheduleone-error-analyzer/dist`.
