import { mkdirSync, readFileSync, readdirSync, rmSync, writeFileSync } from 'node:fs'
import path from 'node:path'
import process from 'node:process'

const repoRoot = process.cwd()
const coreDir = path.join(repoRoot, 'src', 'ErrorAnalyzer.Core')
const csprojPath = path.join(coreDir, 'ErrorAnalyzer.Core.csproj')
const defaultOutputDir = path.join(repoRoot, 'dist', 'core-docs')
const directoryBuildPropsPath = path.join(repoRoot, 'Directory.Build.props')

const args = parseArgs(process.argv.slice(2))
const outputDir = path.resolve(repoRoot, args.output ?? defaultOutputDir)
const basePath = normalizeBasePath(args.base ?? '/')

const projectMetadata = readProjectMetadata(csprojPath, directoryBuildPropsPath)
const sourceFiles = getSourceFiles(coreDir)
const apiDocs = sourceFiles
  .map((filePath) => parseApiFile(filePath))
  .flat()
  .sort((left, right) => left.name.localeCompare(right.name))

const generatedAt = new Date().toISOString().replace('T', ' ').replace(/\.\d+Z$/, ' UTC')

rmSync(outputDir, { recursive: true, force: true })
mkdirSync(outputDir, { recursive: true })
writeFileSync(path.join(outputDir, 'styles.css'), renderStyles(), 'utf8')
writeFileSync(path.join(outputDir, '.nojekyll'), '', 'utf8')
writeFileSync(
  path.join(outputDir, 'index.html'),
  renderHtml({ basePath, projectMetadata, apiDocs, generatedAt }),
  'utf8',
)

console.log(`[core-docs] Generated ${apiDocs.length} API entries at ${path.relative(repoRoot, outputDir)}`)

function parseArgs(argv) {
  const parsed = {}

  for (let index = 0; index < argv.length; index += 1) {
    const current = argv[index]
    if (current === '--output') {
      parsed.output = argv[index + 1]
      index += 1
      continue
    }

    if (current === '--base') {
      parsed.base = argv[index + 1]
      index += 1
    }
  }

  return parsed
}

function normalizeBasePath(value) {
  if (!value || value === '/') {
    return '/'
  }

  const withLeadingSlash = value.startsWith('/') ? value : `/${value}`
  return withLeadingSlash.endsWith('/') ? withLeadingSlash : `${withLeadingSlash}/`
}

function readProjectMetadata(csprojFile, directoryBuildPropsFile) {
  const text = readFileSync(csprojFile, 'utf8')
  const directoryBuildPropsText = readFileSync(directoryBuildPropsFile, 'utf8')
  const versionValue = readXmlValue(text, 'Version') ?? '$(ErrorAnalyzerVersion)'

  return {
    packageId: readXmlValue(text, 'PackageId') ?? 'ErrorAnalyzer.Core',
    version: versionValue === '$(ErrorAnalyzerVersion)' ? (readXmlValue(directoryBuildPropsText, 'ErrorAnalyzerVersion') ?? versionValue) : versionValue,
    title: readXmlValue(text, 'Title') ?? 'ErrorAnalyzer.Core',
    description: readXmlValue(text, 'Description') ?? 'Shared rule-based log analyzer core library.',
    repositoryUrl: readXmlValue(text, 'RepositoryUrl') ?? '',
    targetFramework: readXmlValue(text, 'TargetFramework') ?? 'netstandard2.1',
  }
}

function readXmlValue(text, tagName) {
  const match = text.match(new RegExp(`<${tagName}>([\\s\\S]*?)</${tagName}>`))
  return match?.[1]?.trim() ?? null
}

function getSourceFiles(directory) {
  const entries = readdirSync(directory, { withFileTypes: true })
  return entries.flatMap((entry) => {
    const fullPath = path.join(directory, entry.name)
    if (entry.isDirectory()) {
      if (entry.name === 'bin' || entry.name === 'obj') {
        return []
      }

      return getSourceFiles(fullPath)
    }

    return entry.name.endsWith('.cs') ? [fullPath] : []
  })
}

function parseApiFile(filePath) {
  const text = readFileSync(filePath, 'utf8')
  const lines = text.split(/\r?\n/)
  const entries = []
  let namespaceName = 'ErrorAnalyzer.Core'
  let pendingDoc = []

  for (let index = 0; index < lines.length; index += 1) {
    const trimmed = lines[index].trim()

    if (trimmed.startsWith('namespace ')) {
      namespaceName = trimmed.slice('namespace '.length).replace(/;$/, '').trim()
      continue
    }

    if (trimmed.startsWith('///')) {
      pendingDoc.push(cleanDocLine(trimmed))
      continue
    }

    if (!trimmed) {
      pendingDoc = []
      continue
    }

    if (!trimmed.startsWith('public ')) {
      pendingDoc = []
      continue
    }

    const declaration = collectDeclaration(lines, index)
    const typeMatch = declaration.text.match(/public\s+(?:sealed\s+|static\s+|abstract\s+|partial\s+)*?(record|class|enum)\s+([A-Za-z0-9_]+)/)
    if (!typeMatch) {
      pendingDoc = []
      continue
    }

    const [, kind, name] = typeMatch
    const entry = {
      filePath: path.relative(repoRoot, filePath).replace(/\\/g, '/'),
      namespace: namespaceName,
      kind,
      name,
      signature: cleanupSignature(declaration.text),
      summary: joinDoc(pendingDoc),
      members: [],
      enumMembers: [],
    }

    pendingDoc = []

    if (declaration.openBraceLine >= 0) {
      const body = collectBody(lines, declaration.openBraceLine)
      if (kind === 'enum') {
        entry.enumMembers = parseEnumMembers(body.lines)
      } else {
        entry.members = parseMembers(body.lines, name)
      }

      index = body.endLine
    } else {
      index = declaration.endLine
    }

    entries.push(entry)
  }

  return entries
}

function collectDeclaration(lines, startIndex) {
  const declarationLines = []
  let endLine = startIndex
  let openBraceLine = -1

  for (let index = startIndex; index < lines.length; index += 1) {
    declarationLines.push(lines[index].trim())
    endLine = index

    if (lines[index].includes('{')) {
      openBraceLine = index
      break
    }

    if (lines[index].trim().endsWith(';')) {
      break
    }
  }

  return {
    text: declarationLines.join(' ').replace(/\s+/g, ' ').trim(),
    endLine,
    openBraceLine,
  }
}

function collectBody(lines, openBraceLine) {
  const bodyLines = []
  let depth = 0
  let started = false
  let endLine = openBraceLine

  for (let index = openBraceLine; index < lines.length; index += 1) {
    const line = lines[index]

    for (const character of line) {
      if (character === '{') {
        depth += 1
        started = true
        continue
      }

      if (character === '}') {
        depth -= 1
      }
    }

    if (started) {
      bodyLines.push(line)
    }

    if (started && depth === 0) {
      endLine = index
      break
    }
  }

  return { lines: bodyLines, endLine }
}

function parseEnumMembers(bodyLines) {
  const members = []
  let pendingDoc = []

  for (const line of bodyLines.slice(1, -1)) {
    const trimmed = line.trim()
    if (trimmed.startsWith('///')) {
      pendingDoc.push(cleanDocLine(trimmed))
      continue
    }

    if (!trimmed || trimmed.startsWith('//')) {
      pendingDoc = []
      continue
    }

    const memberMatch = trimmed.match(/^([A-Za-z0-9_]+)\s*,?$/)
    if (!memberMatch) {
      pendingDoc = []
      continue
    }

    members.push({
      name: memberMatch[1],
      summary: joinDoc(pendingDoc),
    })
    pendingDoc = []
  }

  return members
}

function parseMembers(bodyLines, typeName) {
  const members = []
  let pendingDoc = []
  let depth = 0

  for (let index = 1; index < bodyLines.length - 1; index += 1) {
    const line = bodyLines[index]
    const trimmed = line.trim()

    if (trimmed.startsWith('///')) {
      pendingDoc.push(cleanDocLine(trimmed))
      continue
    }

    if (depth === 1 && trimmed.startsWith('public ')) {
      const declaration = collectMemberDeclaration(bodyLines, index)
      members.push({
        kind: classifyMember(declaration.text, typeName),
        signature: cleanupSignature(declaration.text),
        summary: joinDoc(pendingDoc),
      })
      pendingDoc = []
      index = declaration.endIndex
      continue
    }

    if (trimmed && !trimmed.startsWith('//')) {
      pendingDoc = []
    }

    depth += countOccurrences(line, '{')
    depth -= countOccurrences(line, '}')
  }

  return members
}

function collectMemberDeclaration(lines, startIndex) {
  const declarationLines = []
  let endIndex = startIndex

  for (let index = startIndex; index < lines.length; index += 1) {
    const currentLine = lines[index].trim()
    declarationLines.push(currentLine)
    endIndex = index

    if (currentLine.endsWith(';') || currentLine.includes('=>') || currentLine.endsWith('{')) {
      break
    }

    if (currentLine.includes('{ get;') || currentLine.includes('{ get') || currentLine.includes('}')) {
      break
    }
  }

  return {
    text: declarationLines.join(' ').replace(/\s+/g, ' ').trim(),
    endIndex,
  }
}

function classifyMember(signature, typeName) {
  if (signature.includes(' const ')) {
    return 'Constant'
  }

  if (signature.match(new RegExp(`public\\s+${typeName}\\s*\\(`))) {
    return 'Constructor'
  }

  if (signature.includes('(')) {
    return 'Method'
  }

  return 'Property'
}

function cleanDocLine(line) {
  return line.replace(/^\/\/\/\s?/, '').trim()
}

function joinDoc(lines) {
  return lines
    .map((line) => line.replace(/<\/?summary>/g, '').replace(/<\/?remarks>/g, '').trim())
    .filter(Boolean)
    .join(' ')
}

function cleanupSignature(signature) {
  return signature
    .replace(/\s*\{\s*get;\s*\}/g, ' { get; }')
    .replace(/\s+/g, ' ')
    .trim()
}

function countOccurrences(text, character) {
  return [...text].filter((current) => current === character).length
}

function renderHtml({ basePath, projectMetadata, apiDocs, generatedAt }) {
  const overviewCards = [
    {
      title: 'Package',
      value: projectMetadata.packageId,
      body: projectMetadata.description,
    },
    {
      title: 'Target',
      value: projectMetadata.targetFramework,
      body: 'The core library is shared by the plugin host and the browser-hosted WASM analyzer.',
    },
    {
      title: 'Version',
      value: projectMetadata.version,
      body: 'Resolved from the project file and shared build metadata so the docs reflect the package version.',
    },
    {
      title: 'Generated',
      value: generatedAt,
      body: 'This static site is generated directly from the ErrorAnalyzer.Core source tree.',
    },
  ]

  const usageSnippet = `var analyzer = new LogAnalyzer();\nvar result = analyzer.AnalyzeText(File.ReadAllText(\"Latest.log\"), \"Latest.log\");\n\nforeach (var diagnosis in result.Diagnoses)\n{\n    Console.WriteLine($\"{diagnosis.Title}: {diagnosis.SuggestedAction}\");\n}`

  const navLinks = [
    { href: basePath, label: 'Open web analyzer' },
    { href: '#overview', label: 'Overview' },
    { href: '#getting-started', label: 'Getting started' },
    { href: '#api-reference', label: 'API reference' },
  ]

  return `<!DOCTYPE html>
<html lang="en">
  <head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>${escapeHtml(projectMetadata.title)} API Docs</title>
    <link rel="stylesheet" href="styles.css" />
  </head>
  <body>
    <div class="page-shell">
      <header class="hero">
        <p class="eyebrow">ErrorAnalyzer.Core</p>
        <h1>${escapeHtml(projectMetadata.title)} generated docs</h1>
        <p class="hero-copy">${escapeHtml(projectMetadata.description)}</p>
        <nav class="nav-links">
          ${navLinks.map((link) => `<a href="${escapeHtml(link.href)}">${escapeHtml(link.label)}</a>`).join('')}
        </nav>
      </header>

      <main class="content">
        <section id="overview" class="section-block">
          <div class="section-head">
            <p class="section-step">Overview</p>
            <h2>Core library snapshot</h2>
          </div>
          <div class="card-grid">
            ${overviewCards.map((card) => `
              <article class="card">
                <p class="card-label">${escapeHtml(card.title)}</p>
                <h3>${escapeHtml(card.value)}</h3>
                <p>${escapeHtml(card.body)}</p>
              </article>
            `).join('')}
          </div>
          <div class="meta-row">
            <span>Repository:</span>
            <a href="${escapeHtml(projectMetadata.repositoryUrl)}">${escapeHtml(projectMetadata.repositoryUrl)}</a>
          </div>
        </section>

        <section id="getting-started" class="section-block">
          <div class="section-head">
            <p class="section-step">Guide</p>
            <h2>Minimal usage</h2>
          </div>
          <div class="two-column">
            <article class="card">
              <h3>Use the analyzer</h3>
              <p>Create <code>LogAnalyzer</code>, pass log text or a file path, and iterate the resulting diagnoses.</p>
              <pre><code>${escapeHtml(usageSnippet)}</code></pre>
            </article>
            <article class="card">
              <h3>What you get back</h3>
              <ul>
                <li><strong>RuntimeKind</strong> reports whether the log looks like Mono, IL2CPP, or unknown.</li>
                <li><strong>Diagnosis</strong> provides the rule id, mod name, evidence, severity, confidence, and next action.</li>
                <li><strong>DiagnosisAdvice</strong> provides UI-friendly grouping and message priority data for hosts.</li>
              </ul>
            </article>
          </div>
        </section>

        <section id="api-reference" class="section-block">
          <div class="section-head">
            <p class="section-step">Reference</p>
            <h2>Public API</h2>
          </div>
          <div class="api-list">
            ${apiDocs.map((entry) => renderEntry(entry)).join('')}
          </div>
        </section>
      </main>
    </div>
  </body>
</html>`
}

function renderEntry(entry) {
  return `
    <article class="api-entry" id="${escapeHtml(entry.name)}">
      <div class="api-entry-head">
        <div>
          <p class="entry-kind">${escapeHtml(entry.kind)}</p>
          <h3>${escapeHtml(entry.name)}</h3>
        </div>
        <code>${escapeHtml(entry.namespace)}</code>
      </div>
      <pre><code>${escapeHtml(entry.signature)}</code></pre>
      <p>${escapeHtml(entry.summary || 'No XML summary is available for this public type yet.')}</p>
      ${entry.enumMembers.length > 0 ? `
        <div class="member-block">
          <h4>Values</h4>
          <ul class="member-list">
            ${entry.enumMembers.map((member) => `<li><strong>${escapeHtml(member.name)}</strong>${member.summary ? ` — ${escapeHtml(member.summary)}` : ''}</li>`).join('')}
          </ul>
        </div>
      ` : ''}
      ${entry.members.length > 0 ? `
        <div class="member-block">
          <h4>Members</h4>
          <ul class="member-list">
            ${entry.members.map((member) => `<li><span class="member-kind">${escapeHtml(member.kind)}</span><code>${escapeHtml(member.signature)}</code>${member.summary ? `<p>${escapeHtml(member.summary)}</p>` : ''}</li>`).join('')}
          </ul>
        </div>
      ` : ''}
      <p class="source-link">Source: <code>${escapeHtml(entry.filePath)}</code></p>
    </article>
  `
}

function renderStyles() {
  return `:root {
  --bg: #eef7fb;
  --panel: rgba(255, 255, 255, 0.72);
  --text: #315463;
  --text-strong: #153948;
  --text-soft: #5f7d89;
  --border: rgba(21, 57, 72, 0.12);
  --accent: #2a80a8;
  --accent-2: #6bb89c;
  --code-bg: rgba(21, 57, 72, 0.06);
  font: 16px/1.6 Inter, system-ui, sans-serif;
}
* { box-sizing: border-box; }
body {
  margin: 0;
  color: var(--text);
  background:
    radial-gradient(circle at top left, rgba(42, 128, 168, 0.08), transparent 20%),
    radial-gradient(circle at top right, rgba(107, 184, 156, 0.08), transparent 20%),
    var(--bg);
}
code, pre { font-family: "SFMono-Regular", Consolas, monospace; }
a { color: var(--accent); }
.page-shell {
  width: min(1120px, calc(100% - 32px));
  margin: 0 auto;
  padding: 32px 0 48px;
}
.hero, .section-block {
  display: grid;
  gap: 16px;
  padding: 24px;
  border: 1px solid var(--border);
  border-radius: 24px;
  background: var(--panel);
  backdrop-filter: blur(10px);
}
.content { display: grid; gap: 20px; margin-top: 20px; }
.eyebrow, .section-step, .card-label, .entry-kind, .member-kind {
  margin: 0;
  color: var(--text-soft);
  text-transform: uppercase;
  letter-spacing: 0.16em;
  font-size: 0.75rem;
  font-weight: 700;
}
.hero h1, .section-block h2, .api-entry h3, .card h3, .member-block h4 { margin: 0; color: var(--text-strong); }
.hero-copy { max-width: 52rem; margin: 0; }
.nav-links { display: flex; flex-wrap: wrap; gap: 12px; }
.nav-links a {
  padding: 10px 14px;
  border-radius: 999px;
  border: 1px solid var(--border);
  text-decoration: none;
  background: rgba(255, 255, 255, 0.7);
}
.card-grid, .two-column {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 16px;
}
.two-column { grid-template-columns: repeat(2, minmax(0, 1fr)); }
.card, .api-entry {
  display: grid;
  gap: 12px;
  padding: 18px;
  border-radius: 18px;
  border: 1px solid var(--border);
  background: rgba(255, 255, 255, 0.55);
}
.meta-row {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  align-items: center;
}
pre {
  margin: 0;
  overflow-x: auto;
  padding: 14px;
  border-radius: 14px;
  background: var(--code-bg);
}
code {
  padding: 2px 6px;
  border-radius: 8px;
  background: var(--code-bg);
  color: var(--text-strong);
}
pre code { padding: 0; background: transparent; }
.api-list { display: grid; gap: 16px; }
.api-entry-head {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 12px;
}
.member-block { display: grid; gap: 8px; }
.member-list {
  display: grid;
  gap: 10px;
  padding-left: 1.2rem;
  margin: 0;
}
.member-list li { display: grid; gap: 6px; }
.member-kind { display: inline; margin-right: 8px; }
.source-link { color: var(--text-soft); margin: 0; }
@media (max-width: 860px) {
  .card-grid, .two-column { grid-template-columns: 1fr; }
  .api-entry-head { flex-direction: column; }
}
`
}

function escapeHtml(value) {
  return String(value)
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&#39;')
}
