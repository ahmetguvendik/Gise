import { useState } from 'react'
import type { FormEvent } from 'react'
import { ApiError, toApiError } from '../api/http'
import { ErrorAlert } from './ErrorAlert'

interface Props {
  title: string
  description: string
  label: string
  submitLabel: string
  busyLabel: string
  /** Doluysa göndermeden önce tarayıcı onayı istenir. */
  confirm?: string
  /** Geri alınamaz işlem: ayrı ve kırmızı durur. */
  danger?: boolean
  onSubmit: (reason: string) => Promise<unknown>
  onDone: () => void
}

// Sebep zorunlu işlemler: iptal (bilet sahiplerine gider), taslağa geri gönderme (organizatöre gider).
export function ReasonForm({ title, description, label, submitLabel, busyLabel, confirm, danger, onSubmit, onDone }: Props) {
  const [reason, setReason] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<ApiError | null>(null)

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    if (confirm && !window.confirm(confirm)) return
    setBusy(true)
    setError(null)
    try {
      await onSubmit(reason)
      onDone()
    } catch (err) {
      setError(toApiError(err))
      setBusy(false)
    }
  }

  return (
    <form className={danger ? 'form manage__danger' : 'form'} onSubmit={submit}>
      <h3>{title}</h3>
      <p className="muted">{description}</p>

      {error && <ErrorAlert error={error} />}

      <label>
        {label}
        {/* API sınırıyla aynı (CatalogRules). */}
        <textarea value={reason} onChange={(e) => setReason(e.target.value)} maxLength={500} rows={3} required />
      </label>

      <button className={danger ? 'button button--danger' : 'button button--outline'} disabled={busy || reason.trim() === ''}>
        {busy ? busyLabel : submitLabel}
      </button>
    </form>
  )
}
