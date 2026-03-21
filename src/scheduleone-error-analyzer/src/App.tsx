import { useEffect, useMemo, useState, type ChangeEvent, type DragEvent } from 'react'
import './App.css'
import {
  analyzeLogAsync,
  type AnalysisProgress,
  type AnalysisResult,
  type Diagnosis,
} from './lib/analyzer'

function App() {
  const [fileName, setFileName] = useState('No log loaded')
  const [logText, setLogText] = useState('')
  const [result, setResult] = useState<AnalysisResult | null>(null)
  const [analysisProgress, setAnalysisProgress] = useState<AnalysisProgress | null>(null)
  const [isAnalyzing, setIsAnalyzing] = useState(false)
  const [analysisError, setAnalysisError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false

    if (!logText.trim()) {
      return () => {
        cancelled = true
      }
    }

    void analyzeLogAsync(logText, fileName, (progress) => {
      if (!cancelled) {
        setAnalysisProgress(progress)
      }
    })
      .then((analysisResult) => {
        if (cancelled) {
          return
        }

        setResult(analysisResult)
        setIsAnalyzing(false)
        setAnalysisProgress({ phase: 'Done', progress: 1 })
      })
      .catch((error: unknown) => {
        if (cancelled) {
          return
        }

        const message = error instanceof Error ? error.message : 'Unknown analyzer error'
        console.error('[ErrorAnalyzer] Failed to analyze log', error)
        setResult(null)
        setAnalysisError(message)
        setIsAnalyzing(false)
        setAnalysisProgress({ phase: 'Analyzer failed to load', progress: 1 })
      })

    return () => {
      cancelled = true
    }
  }, [fileName, logText])

  const actionCards = useMemo(() => buildActionGroups(result?.diagnoses ?? []), [result])
  const modGroups = useMemo(() => groupDiagnosesByMod(result?.diagnoses ?? []), [result])
  const progressValue = clampProgress(analysisProgress?.progress ?? 0)
  const progressPercent = Math.round(progressValue * 100)

  async function handleFileSelect(file: File | null) {
    if (!file) {
      return
    }

    queueAnalysisState()
    const text = await readFileText(file, (progress) => {
      setAnalysisProgress({
        phase: 'Reading log file',
        progress: Math.max(progress * 0.22, 0.02),
      })
    })

    setAnalysisProgress({ phase: 'Queueing analysis', progress: 0.24 })
    setFileName(file.name)
    setLogText(text)
  }

  function handleDrop(event: DragEvent<HTMLLabelElement>) {
    event.preventDefault()
    void handleFileSelect(event.dataTransfer.files[0] ?? null)
  }

  async function handleInputChange(event: ChangeEvent<HTMLInputElement>) {
    await handleFileSelect(event.target.files?.[0] ?? null)
  }

  async function handlePaste() {
    const text = await navigator.clipboard.readText()
    if (!text.trim()) {
      return
    }

    queueAnalysisState()
    setAnalysisProgress({ phase: 'Preparing pasted log', progress: 0.24 })
    setFileName('Pasted log text')
    setLogText(text)
  }

  function handleClear() {
    resetAnalysisState()
    setFileName('No log loaded')
    setLogText('')
  }

  function resetAnalysisState() {
    setResult(null)
    setAnalysisError(null)
    setAnalysisProgress(null)
    setIsAnalyzing(false)
  }

  function queueAnalysisState() {
    setResult(null)
    setAnalysisError(null)
    setAnalysisProgress({ phase: 'Queued', progress: 0.01 })
    setIsAnalyzing(true)
  }

  return (
    <main className="app-shell">
      <header className="page-header">
        <p className="eyebrow">Schedule 1 Error Analyzer</p>
        <h1>Drop in `Latest.log` and get the first fix to try.</h1>
      </header>

      <section className="section-block">
        <div className="section-head">
          <div>
            <p className="section-step">Step 1</p>
            <h2>Load the log</h2>
          </div>
          <div className="actions">
            <button type="button" onClick={() => void handlePaste()} disabled={isAnalyzing}>
              Paste log
            </button>
            <button type="button" onClick={handleClear} disabled={!logText}>
              Clear
            </button>
          </div>
        </div>

        <label className={`dropzone${isAnalyzing ? ' dropzone-busy' : ''}`} onDragOver={(event) => event.preventDefault()} onDrop={handleDrop}>
          <input type="file" accept=".log,.txt" onChange={handleInputChange} />
          <span className="dropzone-title">Drop `Latest.log` here</span>
          <span className="dropzone-text">or click here to choose a log file</span>
        </label>

        <div className="status-row">
          <div>
            <span className="label">File</span>
            <code>{fileName}</code>
          </div>
          <div>
            <span className="label">Status</span>
            <strong>{isAnalyzing ? analysisProgress?.phase ?? 'Analyzing' : analysisError ? 'Analyzer failed' : result ? 'Ready' : 'Waiting for input'}</strong>
          </div>
          <div>
            <span className="label">Progress</span>
            <strong>{progressPercent}%</strong>
          </div>
        </div>

        <div className="progress-line" aria-hidden="true">
          <span style={{ width: `${progressValue * 100}%` }} />
        </div>
      </section>

      <section className="section-block">
        <div className="section-head">
          <div>
            <p className="section-step">Step 2</p>
            <h2>Do this</h2>
          </div>
        </div>

        {isAnalyzing ? (
          <p className="notice-text">Please wait while the analyzer checks the log.</p>
        ) : analysisError ? (
          <div className="notice-text notice-error">
            <strong>The analyzer could not load.</strong>
            <span>{analysisError}</span>
          </div>
        ) : actionCards.length > 0 ? (
          <div className="action-groups">
            {actionCards.map((group) => (
              <section key={group.key} className="action-group">
                <div className="action-group-head">
                  <p className="action-kicker">{group.urgency}</p>
                  <h3>{group.title}</h3>
                </div>
                <p className="action-primary">{group.primaryAction}</p>
                <p className="action-secondary">{group.explanation}</p>
                <div className="mod-list">
                  {group.mods.map((mod) => (
                    <span key={mod} className="mod-name">
                      {mod}
                    </span>
                  ))}
                </div>
              </section>
            ))}
          </div>
        ) : result ? (
          <p className="notice-text">No common update-related mod break was matched in this log.</p>
        ) : (
          <p className="notice-text">Load a log file and this page will tell you what to try first.</p>
        )}
      </section>

      <section className="section-block details-block">
        <div className="section-head">
          <div>
            <p className="section-step">Optional</p>
            <h2>More details</h2>
          </div>
        </div>

        {modGroups.length > 0 ? (
          <div className="details-list">
            {modGroups.map(([modName, diagnoses]) => {
              const primary = choosePrimaryDiagnosis(diagnoses)
              const detailSummaries = buildDetailSummaries(diagnoses)

              return (
                <details key={modName} className="detail-item">
                  <summary>
                    <span>{modName}</span>
                    <span>{primary.advice.title}</span>
                  </summary>
                  <div className="detail-body">
                    <p>{primary.message}</p>
                    <p className="detail-action">{primary.advice.primaryAction}</p>
                    {detailSummaries.map((summary) => (
                      <div key={summary.key} className="detail-evidence-block">
                        <p className="detail-meta">
                          {summary.title}
                          {summary.totalOccurrences > 1 ? ` - repeated ${summary.totalOccurrences} times` : ''}
                        </p>
                        {summary.evidenceSamples.map((sample) => (
                          <code key={sample} className="evidence">
                            {sample}
                          </code>
                        ))}
                        {summary.hiddenEvidenceCount > 0 ? (
                          <p className="detail-extra-count">
                            +{summary.hiddenEvidenceCount} more unique {summary.hiddenEvidenceCount === 1 ? 'example' : 'examples'} hidden
                          </p>
                        ) : null}
                      </div>
                    ))}
                  </div>
                </details>
              )
            })}
          </div>
        ) : (
          <p className="notice-text subtle-text">Detailed findings will appear here if they are needed.</p>
        )}
      </section>
    </main>
  )
}

function clampProgress(progress: number) {
  return Math.min(Math.max(progress, 0), 1)
}

type ActionGroup = {
  key: string
  priority: number
  urgency: string
  title: string
  primaryAction: string
  explanation: string
  mods: string[]
}

type DetailSummary = {
  key: string
  title: string
  totalOccurrences: number
  evidenceSamples: string[]
  hiddenEvidenceCount: number
}

function buildActionGroups(diagnoses: Diagnosis[]): ActionGroup[] {
  const groups = new Map<string, ActionGroup>()

  for (const [modName, modDiagnoses] of groupDiagnosesByMod(diagnoses)) {
    const primary = choosePrimaryDiagnosis(modDiagnoses)
    const entry = getFriendlyActionGroup(primary, modName)
    const existing = groups.get(entry.key)

    if (existing) {
      if (!existing.mods.includes(modName)) {
        existing.mods.push(modName)
      }
      continue
    }

    groups.set(entry.key, entry)
  }

  return [...groups.values()].sort((left, right) => left.priority - right.priority)
}

function getFriendlyActionGroup(diagnosis: Diagnosis, modName: string): ActionGroup {
  return {
    key: diagnosis.advice.groupKey,
    priority: diagnosis.advice.priority,
    urgency: diagnosis.advice.urgency,
    title: diagnosis.advice.title,
    primaryAction: diagnosis.advice.primaryAction,
    explanation: diagnosis.advice.explanation,
    mods: [modName],
  }
}

function groupDiagnosesByMod(diagnoses: Diagnosis[]) {
  const groups = new Map<string, Diagnosis[]>()

  for (const diagnosis of diagnoses) {
    const key = diagnosis.modName ?? 'Could not identify mod name'
    const current = groups.get(key) ?? []
    current.push(diagnosis)
    groups.set(key, current)
  }

  return [...groups.entries()].sort((left, right) => right[1].length - left[1].length)
}

function choosePrimaryDiagnosis(diagnoses: Diagnosis[]) {
  return [...diagnoses].sort((left, right) => left.advice.priority - right.advice.priority)[0]
}

function buildDetailSummaries(diagnoses: Diagnosis[]): DetailSummary[] {
  const grouped = new Map<string, { title: string; totalOccurrences: number; evidenceSamples: string[] }>()

  for (const diagnosis of diagnoses) {
    const key = `${diagnosis.advice.groupKey}|${diagnosis.title}|${diagnosis.suggestedAction}`
    const existing = grouped.get(key)

    if (existing) {
      existing.totalOccurrences += diagnosis.occurrenceCount
      if (!existing.evidenceSamples.includes(diagnosis.evidence)) {
        existing.evidenceSamples.push(diagnosis.evidence)
      }
      continue
    }

    grouped.set(key, {
      title: diagnosis.title,
      totalOccurrences: diagnosis.occurrenceCount,
      evidenceSamples: [diagnosis.evidence],
    })
  }

  return [...grouped.entries()].map(([key, summary]) => ({
    key,
    title: summary.title,
    totalOccurrences: summary.totalOccurrences,
    evidenceSamples: summary.evidenceSamples.slice(0, 6),
    hiddenEvidenceCount: Math.max(summary.evidenceSamples.length - 6, 0),
  }))
}

function readFileText(file: File, onProgress?: (progress: number) => void) {
  return new Promise<string>((resolve, reject) => {
    const reader = new FileReader()

    reader.onprogress = (event) => {
      if (event.lengthComputable) {
        onProgress?.(event.loaded / event.total)
      }
    }

    reader.onload = () => {
      resolve(typeof reader.result === 'string' ? reader.result : '')
    }

    reader.onerror = () => {
      reject(reader.error ?? new Error('Could not read the selected file.'))
    }

    reader.readAsText(file)
  })
}

export default App
