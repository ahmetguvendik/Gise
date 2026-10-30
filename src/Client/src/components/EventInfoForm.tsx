import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { ApiError, toApiError } from '../api/http'
import { eventsApi } from '../api/events'
import { venuesApi } from '../api/venues'
import { toDateTimeLocal } from '../lib/format'
import { LISTING_DELAY_NOTE } from '../lib/listing'
import type { EventDetail, VenueSummary } from '../types'
import { ErrorAlert } from './ErrorAlert'

interface Props {
  event: EventDetail
  venueName: string
  token: string
  onSaved: () => void
}

// Ad ve tarih her zaman değişir; mekân yalnızca taslakta, çünkü yayından sonra koltuk stoğu mekâna bağlıdır.
export function EventInfoForm({ event, venueName, token, onSaved }: Props) {
  const isDraft = event.status === 'Draft'
  const [title, setTitle] = useState(event.title)
  const [venueId, setVenueId] = useState(event.venueId)
  const [startsAt, setStartsAt] = useState(() => toDateTimeLocal(event.startsAt))
  const [venues, setVenues] = useState<VenueSummary[]>([])
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<ApiError | null>(null)
  const [saved, setSaved] = useState(false)

  useEffect(() => {
    if (!isDraft) return
    let ignore = false
    venuesApi
      .list()
      .then((data) => {
        if (!ignore) setVenues(data)
      })
      .catch((err) => {
        if (!ignore) setError(toApiError(err))
      })
    return () => {
      ignore = true
    }
  }, [isDraft])

  const venueChanged = venueId !== event.venueId
  const dirty = title.trim() !== event.title || venueChanged || startsAt !== toDateTimeLocal(event.startsAt)

  const change = (apply: () => void) => {
    apply()
    setSaved(false)
  }

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    if (venueChanged && event.prices.length > 0 && !window.confirm('Mekân değişince girilen fiyatlar silinir. Devam edilsin mi?')) return
    setBusy(true)
    setError(null)
    try {
      // datetime-local saat dilimi taşımaz; tarayıcının yerel saati UTC'ye çevrilerek gönderilir.
      await eventsApi.update(event.id, { title, venueId, startsAt: new Date(startsAt).toISOString() }, token)
      setSaved(true)
      onSaved()
    } catch (err) {
      setError(toApiError(err))
    } finally {
      setBusy(false)
    }
  }

  return (
    <form className="form" onSubmit={submit}>
      <h3>Bilgiler</h3>

      {error && <ErrorAlert error={error} />}
      {saved && <div className="notice">Bilgiler kaydedildi.{event.status === 'Published' && ` ${LISTING_DELAY_NOTE}`}</div>}

      <div className="form__row">
        <label>
          Etkinlik adı
          <input value={title} onChange={(e) => change(() => setTitle(e.target.value))} maxLength={200} required />
        </label>
        <label>
          Mekân
          {isDraft ? (
            <select value={venueId} onChange={(e) => change(() => setVenueId(e.target.value))} required>
              {venues.length === 0 && <option value={event.venueId}>{venueName}</option>}
              {venues.map((venue) => (
                <option key={venue.id} value={venue.id}>
                  {venue.name} — {venue.city}
                </option>
              ))}
            </select>
          ) : (
            <input value={venueName} disabled />
          )}
        </label>
        <label>
          Tarih ve saat
          <input type="datetime-local" value={startsAt} onChange={(e) => change(() => setStartsAt(e.target.value))} required />
        </label>

        <button className="button button--contrast" disabled={busy || !dirty}>
          {busy ? 'Kaydediliyor…' : 'Bilgileri kaydet'}
        </button>
      </div>

      {!isDraft && (
        <p className="form__hint">Yayındaki etkinliğin mekânı değiştirilemez; koltuk stoğu bu mekânın koltuklarıyla kuruldu.</p>
      )}
      {isDraft && venueChanged && event.prices.length > 0 && (
        <p className="form__hint form__hint--warning">
          Mekân değişirse girilen fiyatlar silinir; yeni mekânın bölümleri için yeniden girilir.
        </p>
      )}
    </form>
  )
}
