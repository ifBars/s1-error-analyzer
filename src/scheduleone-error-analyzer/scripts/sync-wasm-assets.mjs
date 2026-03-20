import fs from 'node:fs'
import path from 'node:path'

const appRoot = process.cwd()
const wasmBundleDir = path.resolve(
  appRoot,
  '../ErrorAnalyzer.WASM/bin/Release/net8.0/browser-wasm/AppBundle',
)
const publicDir = path.resolve(appRoot, 'public')
const publicFrameworkDir = path.join(publicDir, '_framework')

if (!fs.existsSync(wasmBundleDir)) {
  throw new Error(`WASM bundle not found: ${wasmBundleDir}`)
}

fs.mkdirSync(publicDir, { recursive: true })
fs.rmSync(publicFrameworkDir, { recursive: true, force: true })
fs.mkdirSync(publicFrameworkDir, { recursive: true })

fs.cpSync(path.join(wasmBundleDir, '_framework'), publicFrameworkDir, { recursive: true })
fs.copyFileSync(
  path.join(wasmBundleDir, 'ErrorAnalyzer.WASM.runtimeconfig.json'),
  path.join(publicDir, 'ErrorAnalyzer.WASM.runtimeconfig.json'),
)

console.log('[ErrorAnalyzer] Synced WASM assets into public/')
