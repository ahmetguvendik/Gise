import { useState } from 'react'
import type { FormEvent } from 'react'
import { ApiError } from '../api/http'
import { eventsApi } from '../api/events'
import type { VenueSummary } from '../types'

interface Props {
  token: string
  venues: VenueSummary[]
}

export function CreateEventForm({ token, venues }: Props) {
  const [title, setTitle] = useState('')
  const [venueId, setVenueId] = useState('')
  const [startsAt, setStartsAt] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<ApiError | null>(null)
  const [created, setCreated] = useState<string | null>(null)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setBusy(true)
    setError(null)
    setCreated(null)
    try {
      // datetime-local saat dilimi taşımaz; tarayıcının yerel saati UTC'ye çevrilerek gönderilir.
      const result = await eventsApi.create({ title, venueId, startsAt: new Date(startsAt).toISOString() }, token)
      setCreated(result.title)
      setTitle('')
      setVenueId('')
      setStartsAt('')
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

      {created && (
        <div className="notice">
          “{created}” taslak olarak oluşturuldu. Fiyatlar girilip yayınlanınca listede görünür.
        </div>
      )}

      <label>
        Etkinlik adı
        <input value={title} onChange={(e) => setTitle(e.target.value)} maxLength={200} required />
      </label>
      <label>
        Mekân
        <select value={venueId} onChange={(e) => setVenueId(e.target.value)} required disabled={venues.length === 0}>
          <option value="" disabled>
            {venues.length === 0 ? 'Henüz mekân yok; mekânları admin ekler' : 'Mekân seçin'}
          </option>
          {venues.map((venue) => (
            <option key={venue.id} value={venue.id}>
              {venue.name} — {venue.city}
            </option>
          ))}
        </select>
      </label>
      <label>
        Tarih ve saat
        <input type="datetime-local" value={startsAt} onChange={(e) => setStartsAt(e.target.value)} required />
      </label>

      <button className="button" disabled={busy || venues.length === 0}>
        {busy ? 'Kaydediliyor…' : 'Oluştur'}
      </button>
    </form>
  )
}
