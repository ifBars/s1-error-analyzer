# Schedule 1 Error Analyzer Web UI

This is the Vite + React frontend for the browser-hosted Schedule 1 error analyzer.

## Local development

Preferred from the repository root:

```bash
bun install
bun run dev
```

Or directly from this package:

```bash
bun install
bun run dev
```

`predev` publishes the WASM project and syncs `_framework/` assets into `public/` before Vite starts.

## Production build

From the repository root:

```bash
bun run build
```

Or from this package:

```bash
bun run build
```

This also republishes the WASM analyzer first and copies the runtime assets into `public/`.

## GitHub Pages

The app is ready to build for either:

- a user/org site at `/`
- a project site at `/<repo-name>/`

### Project site build

If your repository will be published at `https://<user>.github.io/ErrorAnalyzer/`, run:

From the repository root:

```bash
bun run build:pages ErrorAnalyzer
```

Or from this package:

```bash
bun run build:pages ErrorAnalyzer
```

That script:

- republishes the WASM analyzer
- syncs `_framework/` assets into `public/`
- builds Vite with `base=/ErrorAnalyzer/`

### User/org site build

If you are deploying to the root GitHub Pages domain, run:

```bash
bun run build:pages
```

## Publish checklist

- confirm the repository path you want to host under
- build with `bun run build:pages <repo-name>` for a project site, or `bun run build:pages` for root
- upload or deploy the generated `dist/` directory
- in GitHub Pages settings, point deployment to the built static site

## Notes

- The frontend uses `import.meta.env.BASE_URL` when loading the .NET `_framework/` files, so the WASM runtime and static assets follow the GitHub Pages subpath correctly.
- `scripts/build-pages.mjs` also accepts `GITHUB_PAGES_REPO` if you prefer setting the repo name from CI instead of passing it as a CLI argument.
