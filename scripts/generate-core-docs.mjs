import { cpSync, existsSync, mkdirSync, rmSync } from 'node:fs'
import { spawnSync } from 'node:child_process'
import path from 'node:path'
import process from 'node:process'

const repoRoot = process.cwd()
const docfxConfigPath = path.join(repoRoot, '.docfx', 'docfx.json')
const templatesRoot = path.join(repoRoot, '.docfx', 'templates')
const defaultOutputDir = path.join(repoRoot, 'dist', 'core-docs')

const args = parseArgs(process.argv.slice(2))
const outputDir = path.resolve(repoRoot, args.output ?? defaultOutputDir)
validateOutputDir(outputDir, args.output, defaultOutputDir)

if (args.base) {
  console.log(
    '[core-docs] The --base argument is not used for DocFX generation and is now ignored. Set endpoint in the frontend via VITE_CORE_DOCS_PATH if needed.',
  )
}

ensureDocfxTooling()
ensureMaterialTheme()
rmSync(outputDir, { recursive: true, force: true })
run('dotnet', ['tool', 'run', 'docfx', 'build', docfxConfigPath, '--output', outputDir])

console.log(`[core-docs] Generated DocFX docs at ${path.relative(repoRoot, outputDir)}`)

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

function validateOutputDir(outputDir, userOutput, fallbackOutputDir) {
  const outputDirRelative = path.relative(repoRoot, outputDir)
  const outputDirPathEscapesRepo =
    outputDirRelative === '' ||
    outputDirRelative === '.' ||
    outputDirRelative === '..' ||
    outputDirRelative.startsWith(`..${path.sep}`) ||
    path.isAbsolute(outputDirRelative) ||
    path.parse(outputDir).root === path.normalize(outputDir)

  if (outputDirPathEscapesRepo) {
    throw new Error(
      `[core-docs] Refusing to build to unsafe output directory ${outputDir}. args.output=${userOutput ?? '<not set>'}, defaultOutputDir=${fallbackOutputDir}, repoRoot=${repoRoot}. Output path must be inside this repository and must not be the repository root.`,
    )
  }
}

function ensureDocfxTooling() {
  const toolManifest = path.join(repoRoot, '.config', 'dotnet-tools.json')
  if (!existsSync(toolManifest)) {
    throw new Error('[core-docs] Missing .config/dotnet-tools.json. Add docfx to a local dotnet tool manifest and retry.')
  }

  run('dotnet', ['tool', 'restore'])
}

function ensureMaterialTheme() {
  const materialThemeDir = path.join(templatesRoot, 'material')
  if (existsSync(materialThemeDir)) {
    return
  }

  const tempCheckout = path.join(templatesRoot, 'docfx-material')
  if (existsSync(tempCheckout)) {
    rmSync(tempCheckout, { recursive: true, force: true })
  }

  mkdirSync(templatesRoot, { recursive: true })
  run('git', ['clone', '--depth', '1', 'https://github.com/ovasquez/docfx-material.git', tempCheckout])
  if (!existsSync(path.join(tempCheckout, 'material'))) {
    throw new Error('[core-docs] Could not locate the Material template folder in the fetched template source.')
  }

  cpSync(path.join(tempCheckout, 'material'), materialThemeDir, { recursive: true })
  rmSync(tempCheckout, { recursive: true, force: true })
}

function run(command, args) {
  const result = spawnSync(command, args, {
    stdio: 'inherit',
    shell: process.platform === 'win32',
  })

  if (result.status !== 0) {
    process.exit(result.status ?? 1)
  }
}
