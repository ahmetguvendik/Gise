import { useState } from 'react'
import type { FormEvent } from 'react'
import { ApiError, toApiError } from '../api/http'
import { eventsApi } from '../api/events'
import { LISTING_DELAY_NOTE } from '../lib/listing'
import type { EventPrice, VenueSection } from '../types'
import { ErrorAlert } from './ErrorAlert'

interface Props {
  eventId: string
  token: string
  sections: VenueSection[]
  prices: EventPrice[]
  /** Yayındaki etkinlikte ana sayfanın gecikmeyle güncelleneceği yazılır. */
  published?: boolean
  onSaved: () => void
}

// API alan hatalarını "prices[1].price" diye döner; kullanıcıya hangi bölüm olduğu söylenir.
function withSectionNames(error: ApiError, submitted: VenueSection[]) {
  const fieldErrors = Object.fromEntries(
    Object.entries(error.fieldErrors).map(([key, messages]) => {
      const section = submitted[Number(/^prices\[(\d+)\]/.exec(key)?.[1])]
      return [key, section ? messages.map((message) => `${section.name}: ${message}`) : messages]
    }),
  )
  return new ApiError(error.status, error.message, fieldErrors, error.detail)
}

// Her bölüm için bir fiyat. Boş bırakılan bölüm listeye girmez; kayıtlı fiyatı varsa kalkar.
export function PriceForm({ eventId, token, sections, prices, published = false, onSaved }: Props) {
  const [values, setValues] = useState<Record<string, string>>(() =>
    Object.fromEntries(sections.map((s) => [s.id, prices.find((p) => p.sectionId === s.id)?.price.toString() ?? ''])),
  )
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<ApiError | null>(null)
  const [saved, setSaved] = useState(false)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setBusy(true)
    setError(null)
    setSaved(false)
    const filled = sections.filter((s) => values[s.id].trim() !== '')
    try {
      const items = filled.map((s) => ({ sectionId: s.id, price: Number(values[s.id].replace(',', '.')) }))
      await eventsApi.setPrices(eventId, items, token)
      setSaved(true)
      onSaved()
    } catch (err) {
      setError(withSectionNames(toApiError(err), filled))
    } finally {
      setBusy(false)
    }
  }

  return (
    <form className="form" onSubmit={submit}>
      <h3>Fiyatlar</h3>

      {error && <ErrorAlert error={error} />}
      {saved && <div className="notice">Fiyatlar kaydedildi.{published && ` ${LISTING_DELAY_NOTE}`}</div>}

      <div className="form__row">
        {sections.map((section) => (
          <label key={section.id}>
            {section.name} (₺)
            <input
              type="number"
              min="0"
              step="0.01"
              inputMode="decimal"
              value={values[section.id]}
              onChange={(e) => setValues((current) => ({ ...current, [section.id]: e.target.value }))}
            />
          </label>
        ))}

        <button className="button button--contrast" disabled={busy}>
          {busy ? 'Kaydediliyor…' : 'Fiyatları kaydet'}
        </button>
      </div>
    </form>
  )
}
