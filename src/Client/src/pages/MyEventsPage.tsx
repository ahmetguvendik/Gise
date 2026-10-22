import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from 'react-oidc-context'
import { ApiError, toApiError } from '../api/http'
import { eventsApi } from '../api/events'
import { venuesApi } from '../api/venues'
import { isAdmin } from '../auth/roles'
import { CreateEventForm } from '../components/CreateEventForm'
import { ErrorAlert } from '../components/ErrorAlert'
import { EventCard } from '../components/EventCard'
import type { EventStatus, MyEvent, VenueSummary } from '../types'

// Guard'ın arkasında: kullanıcı giriş yapmış ve organizatör ya da admin.
export function MyEventsPage() {
  const auth = useAuth()
  const navigate = useNavigate()
  const token = auth.user?.access_token ?? ''
  const [events, setEvents] = useState<MyEvent[] | null>(null)
  const [venues, setVenues] = useState<VenueSummary[]>([])
  const [error, setError] = useState<ApiError | null>(null)
  // Organizatör yalnızca taslağı değiştirir; yayındakini admin yönetir.
  const actionLabel = (status: EventStatus) =>
    status === 'Draft' ? 'Düzenle →' : status === 'Published' && isAdmin(auth.user) ? 'Yönet →' : 'Görüntüle →'

  useEffect(() => {
    let ignore = false
    Promise.all([eventsApi.mine(token), venuesApi.list()]).then(
      ([eventData, venueData]) => {
        if (ignore) return
        setEvents(eventData)
        setVenues(venueData)
        setError(null)
      },
      (err) => {
        if (!ignore) setError(toApiError(err))
      },
    )
    return () => {
      ignore = true
    }
  }, [token])

  return (
    <div className="container page">
      <div className="stack">
        <p className="eyebrow">Organizatör</p>
        <h1 className="page-title">Etkinliklerim</h1>
      </div>

      {error && <ErrorAlert error={error} />}

      <CreateEventForm token={token} venues={venues} onCreated={(id) => navigate(`/etkinlik/${id}`)} />

      <section className="stack" aria-labelledby="tum-etkinlikler">
        <h2 id="tum-etkinlikler" className="section-title">
          Tüm etkinlikler
        </h2>
        {events === null && !error ? (
          <ul className="event-grid" aria-label="Yükleniyor">
            <li className="skeleton" />
            <li className="skeleton" />
            <li className="skeleton" />
          </ul>
        ) : events && events.length === 0 ? (
          <p className="muted">Henüz etkinliğiniz yok. Yukarıdaki formla ilkini oluşturun.</p>
        ) : (
          events && (
            <ul className="event-grid">
              {events.map((event) => (
                <EventCard
                  key={event.id}
                  id={event.id}
                  title={event.title}
                  startsAt={event.startsAt}
                  venue={`${event.venueName}, ${event.city}`}
                  minPrice={event.minPrice}
                  posterUrl={event.posterUrl}
                  posterSrcset={event.posterSrcset}
                  status={event.status}
                  actionLabel={actionLabel(event.status)}
                />
              ))}
            </ul>
          )
        )}
      </section>
    </div>
  )
}
