import { useState } from 'react'
import type { FormEvent } from 'react'
import { ApiError } from '../api/http'
import { eventsApi } from '../api/events'

export function CreateEventForm({ token, onCreated }: { token: string; onCreated: () => void }) {
  const [title, setTitle] = useState('')
  const [venue, setVenue] = useState('')
  const [startsAt, setStartsAt] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<ApiError | null>(null)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setBusy(true)
    setError(null)
    try {
      // datetime-local saat dilimi taşımaz; tarayıcının yerel saati UTC'ye çevrilerek gönderilir.
      await eventsApi.create({ title, venue, startsAt: new Date(startsAt).toISOString() }, token)
      setTitle('')
      setVenue('')
      setStartsAt('')
      onCreated()
    } catch (err) {
      setError(err instanceof ApiError ? err : new ApiError(0, 'Beklenmeyen bir hata oluştu.'))
    } finally {
      setBusy(false)
    }
  }

  return (
    <form className="card form" onSubmit={submit}>
      <h2>Yeni etkinlik</h2>

      {error && (
        <div className="alert">
          <strong>{error.message}</strong>
          {Object.values(error.fieldErrors).flat().map((message) => (
            <div key={message}>{message}</div>
          ))}
        </div>
      )}

      <label>
        Etkinlik adı
        <input value={title} onChange={(e) => setTitle(e.target.value)} maxLength={200} required />
      </label>
      <label>
        Mekân
        <input value={venue} onChange={(e) => setVenue(e.target.value)} maxLength={200} required />
      </label>
      <label>
        Tarih ve saat
        <input type="datetime-local" value={startsAt} onChange={(e) => setStartsAt(e.target.value)} required />
      </label>

      <button className="button" disabled={busy}>
        {busy ? 'Kaydediliyor…' : 'Oluştur'}
      </button>
    </form>
  )
}
