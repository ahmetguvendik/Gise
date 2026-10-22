import { useState } from 'react'
import type { FormEvent } from 'react'
import { ApiError, toApiError } from '../api/http'
import { eventsApi } from '../api/events'
import { ErrorAlert } from './ErrorAlert'

interface Props {
  eventId: string
  title: string
  token: string
  onCancelled: () => void
}

// İptal geri alınamaz: sebep zorunlu ve son bir onay istenir.
export function CancelEventForm({ eventId, title, token, onCancelled }: Props) {
  const [reason, setReason] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<ApiError | null>(null)

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    if (!window.confirm(`"${title}" iptal edilsin mi? Bu geri alınamaz; etkinlik satıştan ve listeden kalkar.`)) return
    setBusy(true)
    setError(null)
    try {
      await eventsApi.cancel(eventId, reason, token)
      onCancelled()
    } catch (err) {
      setError(toApiError(err))
      setBusy(false)
    }
  }

  return (
    <form className="form manage__danger" onSubmit={submit}>
      <h3>Etkinliği iptal et</h3>
      <p className="muted">
        İptal geri alınamaz. Etkinlik listeden kalkar; sayfası sebebiyle birlikte "iptal edildi" olarak kalır.
      </p>

      {error && <ErrorAlert error={error} />}

      <label>
        İptal sebebi
        <textarea value={reason} onChange={(e) => setReason(e.target.value)} maxLength={500} rows={3} required />
      </label>

      <button className="button button--danger" disabled={busy || reason.trim() === ''}>
        {busy ? 'İptal ediliyor…' : 'Etkinliği iptal et'}
      </button>
    </form>
  )
}
