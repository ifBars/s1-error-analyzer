import { mkdirSync, writeFileSync } from 'node:fs'
import path from 'node:path'
import { spawnSync } from 'node:child_process'

const repoName = process.argv[2] ?? process.env.GITHUB_PAGES_REPO ?? ''
const basePath = repoName ? `/${repoName.replace(/^\/+|\/+$/g, '')}/` : '/'
const distDir = path.join(process.cwd(), 'src', 'scheduleone-error-analyzer', 'dist')
const docsOutputDir = path.join(distDir, 'docs', 'core')

run('bun', ['scripts/run-frontend.mjs', 'build:pages', repoName].filter(Boolean))
run('node', ['scripts/generate-core-docs.mjs', '--base', basePath, '--output', docsOutputDir])
mkdirSync(distDir, { recursive: true })
writeFileSync(path.join(distDir, '.nojekyll'), '', 'utf8')

console.log(`[pages-site] Built combined analyzer app and core docs with base path ${basePath}`)

function run(command, args) {
  const result = spawnSync(command, args, {
    stdio: 'inherit',
    shell: process.platform === 'win32',
  })

  if (result.status !== 0) {
    process.exit(result.status ?? 1)
  }
}
