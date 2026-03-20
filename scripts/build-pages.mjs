import { spawnSync } from 'node:child_process'

const repoName = process.argv[2] ?? process.env.GITHUB_PAGES_REPO ?? ''
const basePath = repoName ? `/${repoName.replace(/^\/+|\/+$/g, '')}/` : '/'

run('bun', ['run', 'prebuild'])
run('bun', ['x', 'tsc', '-b'])
run('bun', ['x', 'vite', 'build'], {
  env: {
    ...process.env,
    GITHUB_PAGES_BASE_PATH: basePath,
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
