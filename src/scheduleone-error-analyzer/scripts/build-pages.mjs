import { spawnSync } from 'node:child_process'

const repoName = process.argv[2] ?? process.env.GITHUB_PAGES_REPO ?? ''
const basePath = repoName ? `/${repoName.replace(/^\/+|\/+$/g, '')}/` : '/'
const docsEndpoint = normalizeDocsPath(process.env.CORE_DOCS_ENDPOINT ?? 'docs')
const docsPublicPath = `${basePath}${docsEndpoint}`.replace(/\/?$/, '/')

run('bun', ['run', 'prebuild'])
run('bun', ['x', 'tsc', '-b'])
run('bun', ['x', 'vite', 'build'], {
  env: {
    ...process.env,
    GITHUB_PAGES_BASE_PATH: basePath,
    CORE_DOCS_ENDPOINT: docsEndpoint,
    CORE_DOCS_PUBLIC_PATH: docsPublicPath,
    VITE_CORE_DOCS_PATH: docsPublicPath,
  },
})

console.log(`[ErrorAnalyzer] Built GitHub Pages bundle with base path ${basePath}`)

function run(command, args, options = {}) {
  const result = spawnSync(command, args, {
    stdio: 'inherit',
    shell: process.platform === 'win32',
    ...options,
  })

  if (result.status !== 0) {
    process.exit(result.status ?? 1)
  }
}

function normalizeDocsPath(value) {
  return value.replace(/^\/+|\/+$/g, '')
}
