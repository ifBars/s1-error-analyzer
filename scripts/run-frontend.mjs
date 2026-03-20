import { spawnSync } from 'node:child_process'

const [scriptName, ...args] = process.argv.slice(2)

if (!scriptName) {
  console.error('Expected a frontend script name.')
  process.exit(1)
}

const result = spawnSync('bun', ['run', scriptName, ...args], {
  cwd: new URL('../src/scheduleone-error-analyzer/', import.meta.url),
  stdio: 'inherit',
  shell: process.platform === 'win32',
})

if (result.status !== 0) {
  process.exit(result.status ?? 1)
}
