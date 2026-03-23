import { mkdirSync, writeFileSync } from 'node:fs'
import path from 'node:path'
import { spawnSync } from 'node:child_process'

const cli = parseArgs(process.argv.slice(2))
const repoName = cli.repoName ?? process.env.GITHUB_PAGES_REPO ?? ''
const docsEndpoint = normalizeDocsPath(cli.docsEndpoint ?? process.env.CORE_DOCS_ENDPOINT ?? 'docs')
const basePath = repoName ? `/${repoName.replace(/^\/+|\/+$/g, '')}/` : '/'
const distDir = path.join(process.cwd(), 'src', 'scheduleone-error-analyzer', 'dist')
const docsOutputDir = path.join(distDir, docsEndpoint)
run('bun', ['scripts/run-frontend.mjs', 'build:pages', repoName].filter(Boolean), {
  env: {
    ...process.env,
    CORE_DOCS_ENDPOINT: docsEndpoint,
    CORE_DOCS_PUBLIC_PATH: `${basePath}${docsEndpoint}`.replace(/\/?$/, '/'),
  },
})
run('bun', ['scripts/generate-core-docs.mjs', '--base', basePath, '--output', docsOutputDir], {
  env: {
    ...process.env,
    CORE_DOCS_ENDPOINT: docsEndpoint,
  },
})
mkdirSync(distDir, { recursive: true })
writeFileSync(path.join(distDir, '.nojekyll'), '', 'utf8')

console.log(`[pages-site] Built combined analyzer app and core docs endpoint ${docsEndpoint} with base path ${basePath}`)

function run(command, args) {
  const result = spawnSync(command, args, {
    stdio: 'inherit',
    shell: process.platform === 'win32',
    ...arguments[2],
  })

  if (result.status !== 0) {
    process.exit(result.status ?? 1)
  }
}

function normalizeDocsPath(value) {
  return value.replace(/^\/+|\/+$/g, '')
}

function parseArgs(argv) {
  const parsed = {}

  for (let index = 0; index < argv.length; index += 1) {
    if (!argv[index].startsWith('-') && !parsed.repoName) {
      parsed.repoName = argv[index]
      continue
    }

    if (argv[index] === '--docs-endpoint') {
      parsed.docsEndpoint = argv[index + 1]
      index += 1
      continue
    }
  }

  return parsed
}
