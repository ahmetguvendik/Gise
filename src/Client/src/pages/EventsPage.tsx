import { useEffect, useState } from 'react'
import { toApiError } from '../api/http'
import { eventsApi } from '../api/events'
import { venuesApi } from '../api/venues'
import { EventList } from '../components/EventList'
import type { EventSummary, VenueSummary } from '../types'

export function EventsPage() {
  const [events, setEvents] = useState<EventSummary[] | null>(null)
  const [venues, setVenues] = useState<VenueSummary[]>([])
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    // Geç gelen eski bir yanıt, yenisinin üzerine yazmasın.
    let ignore = false
    // Liste yalnızca mekân ID'si döndürüyor; ad ve şehir mekân listesinden eşleştirilir.
    Promise.all([eventsApi.list(), venuesApi.list()]).then(
      ([eventData, venueData]) => {
        if (ignore) return
        setEvents(eventData)
        setVenues(venueData)
        setError(null)
      },
      (err) => {
        if (!ignore) setError(toApiError(err).message)
      },
    )
    return () => {
      ignore = true
    }
  }, [])

  return (
    <>
      <h1>Yaklaşan etkinlikler</h1>

      {error && <div className="alert">{error}</div>}
      {events === null && !error ? <p className="muted">Yükleniyor…</p> : events && <EventList events={events} venues={venues} />}
    </>
  )
}
