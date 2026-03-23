export type RuntimeKind = 'Unknown' | 'Mono' | 'Il2Cpp'
export type DiagnosisSeverity = 'Info' | 'Warning' | 'Error'
export type DiagnosisConfidence = 'Low' | 'Medium' | 'High'
export type DiagnosisAdvice = {
  groupKey: string
  priority: number
  urgency: string
  title: string
  primaryAction: string
  explanation: string
}

export type Diagnosis = {
  ruleId: string
  title: string
  message: string
  suggestedAction: string
  modName: string | null
  evidence: string
  lineNumber: number
  severity: DiagnosisSeverity
  confidence: DiagnosisConfidence
  occurrenceCount: number
  advice: DiagnosisAdvice
}

export type AnalysisResult = {
  runtime: RuntimeKind
  diagnoses: Diagnosis[]
}

export type AnalysisProgress = {
  phase: string
  progress: number
}

type AnalyzerExports = {
  ErrorAnalyzer: {
    WASM: {
      AnalyzerExports: {
        AnalyzeLogAsync: (text: string) => Promise<string>
        GetVersion: () => string
      }
    }
  }
}

type DotnetModule = {
  withDiagnosticTracing: (enabled: boolean) => {
    create: (options: { locateFile: (path: string) => string; configSrc?: string }) => Promise<{
      getAssemblyExports: (assemblyName: string) => Promise<AnalyzerExports>
      getConfig: () => { mainAssemblyName: string }
    }>
  }
}

let exportsPromise: Promise<AnalyzerExports> | null = null
const ANALYZER_PROGRESS_KEY = '__scheduleOneAnalyzerReportProgress'
const ANALYZER_YIELD_KEY = '__scheduleOneAnalyzerYieldToUi'

export async function analyzeLogAsync(
  text: string,
  onProgress?: (progress: AnalysisProgress) => void,
): Promise<AnalysisResult> {
  console.info('[ErrorAnalyzer] Starting log analysis')
  onProgress?.({ phase: 'Booting analyzer', progress: 0.28 })
  const exports = await getAnalyzerExports(onProgress)

  onProgress?.({ phase: 'Running analyzer rules', progress: 0.58 })

  const cleanup = installAnalyzerProgressReporter((progress) => {
    onProgress?.({
      phase: progress.phase,
      progress: scaleProgress(progress.progress, 0.58, 0.98),
    })
  })

  let json = ''

  try {
    json = await exports.ErrorAnalyzer.WASM.AnalyzerExports.AnalyzeLogAsync(text)
    console.info('[ErrorAnalyzer] WASM analysis completed')
  } finally {
    cleanup()
  }

  onProgress?.({ phase: 'Preparing results', progress: 0.99 })
  return JSON.parse(json) as AnalysisResult
}

async function getAnalyzerExports(onProgress?: (progress: AnalysisProgress) => void): Promise<AnalyzerExports> {
  if (exportsPromise) {
    onProgress?.({ phase: 'Reusing loaded runtime', progress: 0.54 })
    return exportsPromise
  }

  exportsPromise = loadAnalyzerExports(onProgress)
  return exportsPromise
}

async function loadAnalyzerExports(onProgress?: (progress: AnalysisProgress) => void): Promise<AnalyzerExports> {
  const dotnetUrl = `${getBaseUrl()}_framework/dotnet.js`
  const runtimeConfigUrl = `${getBaseUrl()}ErrorAnalyzer.WASM.runtimeconfig.json`
  console.info('[ErrorAnalyzer] Loading .NET runtime from', dotnetUrl)
  console.info('[ErrorAnalyzer] Loading runtime config from', runtimeConfigUrl)
  onProgress?.({ phase: 'Fetching .NET runtime', progress: 0.34 })
  const dynamicImport = new Function('url', 'return import(url)') as (url: string) => Promise<{ dotnet?: DotnetModule }>
  const module = await dynamicImport(dotnetUrl)
  const dotnet = module.dotnet

  if (!dotnet) {
    console.error('[ErrorAnalyzer] .NET runtime import did not expose dotnet')
    throw new Error('Could not load .NET WebAssembly runtime.')
  }

  const frameworkPath = `${getBaseUrl()}_framework/`
  onProgress?.({ phase: 'Starting WebAssembly runtime', progress: 0.46 })
  const runtime = await dotnet
    .withDiagnosticTracing(false)
    .create({
      configSrc: runtimeConfigUrl,
      locateFile: (path: string) => frameworkPath + path,
    } as {
      configSrc: string
      locateFile: (path: string) => string
    })

  const mainAssemblyName = runtime.getConfig().mainAssemblyName
  console.info('[ErrorAnalyzer] Loaded main assembly', mainAssemblyName)
  onProgress?.({ phase: 'Loading analyzer assembly', progress: 0.58 })

  return runtime.getAssemblyExports(mainAssemblyName)
}

function installAnalyzerProgressReporter(onProgress: (progress: AnalysisProgress) => void) {
  const globalScope = globalThis as typeof globalThis & {
    [ANALYZER_PROGRESS_KEY]?: (phase: string, progress: number) => void
    [ANALYZER_YIELD_KEY]?: () => Promise<void>
  }

  globalScope[ANALYZER_PROGRESS_KEY] = (phase: string, progress: number) => {
    onProgress({
      phase,
      progress,
    })
  }

  globalScope[ANALYZER_YIELD_KEY] = () => {
    return new Promise((resolve) => {
      window.requestAnimationFrame(() => {
        window.setTimeout(resolve, 0)
      })
    })
  }

  return () => {
    delete globalScope[ANALYZER_PROGRESS_KEY]
    delete globalScope[ANALYZER_YIELD_KEY]
  }
}

function scaleProgress(progress: number, start: number, end: number) {
  return start + (end - start) * progress
}

function getBaseUrl(): string {
  if (typeof window === 'undefined') {
    return '/'
  }

  const base = import.meta.env.BASE_URL
  const normalized = base && base !== '/' ? (base.endsWith('/') ? base : `${base}/`) : '/'
  return new URL(normalized, window.location.origin).href
}
