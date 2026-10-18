import { useEffect, useState } from 'react'
import { useAuth } from 'react-oidc-context'
import { ApiError } from '../api/http'
import { eventsApi } from '../api/events'
import { venuesApi } from '../api/venues'
import { canManageEvents } from '../auth/roles'
import { CreateEventForm } from '../components/CreateEventForm'
import { EventList } from '../components/EventList'
import type { EventSummary, VenueSummary } from '../types'

export function EventsPage() {
  const auth = useAuth()
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
        if (!ignore) setError(err instanceof ApiError ? err.message : 'Beklenmeyen bir hata oluştu.')
      },
    )
    return () => {
      ignore = true
    }
  }, [])

  const token = auth.user?.access_token

  return (
    <main className="container page">
      <h1>Yaklaşan etkinlikler</h1>

      {error && <div className="alert">{error}</div>}
      {events === null && !error ? <p className="muted">Yükleniyor…</p> : events && <EventList events={events} venues={venues} />}

      {token && canManageEvents(auth.user) && (
        <CreateEventForm token={token} venues={venues} />
      )}
    </main>
  )
}
