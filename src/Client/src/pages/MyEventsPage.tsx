import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from 'react-oidc-context'
import { ApiError, toApiError } from '../api/http'
import { eventsApi } from '../api/events'
import { venuesApi } from '../api/venues'
import { CreateEventForm } from '../components/CreateEventForm'
import { ErrorAlert } from '../components/ErrorAlert'
import { StatusBadge } from '../components/StatusBadge'
import { formatDate, formatPrice } from '../lib/format'
import type { MyEvent, VenueSummary } from '../types'

// Guard'ın arkasında: kullanıcı giriş yapmış ve organizatör ya da admin.
export function MyEventsPage() {
  const auth = useAuth()
  const navigate = useNavigate()
  const token = auth.user?.access_token ?? ''
  const [events, setEvents] = useState<MyEvent[] | null>(null)
  const [venues, setVenues] = useState<VenueSummary[]>([])
  const [error, setError] = useState<ApiError | null>(null)

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
    <>
      <h1>Etkinliklerim</h1>

      <CreateEventForm token={token} venues={venues} onCreated={(id) => navigate(`/etkinlik/${id}`)} />

      {error && <ErrorAlert error={error} />}
      {events === null && !error ? (
        <p className="muted">Yükleniyor…</p>
      ) : events && events.length === 0 ? (
        <p className="muted">Henüz etkinliğiniz yok.</p>
      ) : (
        events && (
          <ul className="event-list">
            {events.map((event) => (
              <li key={event.id} className="event-card">
                <div className="event-card__head">
                  <h3 className="event-card__title">
                    <Link to={`/etkinlik/${event.id}`}>{event.title}</Link>
                  </h3>
                  <StatusBadge status={event.status} />
                </div>
                <p className="muted">
                  {event.venueName}, {event.city} · {formatDate(event.startsAt)}
                </p>
                <p className="event-card__price">
                  {event.minPrice !== null ? `${formatPrice(event.minPrice)}'den başlayan fiyatlarla` : 'Fiyat girilmedi'}
                </p>
                {/* Fiyat girme ve yayınlama detay sayfasında; nereden gidileceği açıkça görünsün. */}
                <Link className="button button--ghost event-card__action" to={`/etkinlik/${event.id}`}>
                  {event.status === 'Draft' ? 'Fiyat ve yayın →' : 'Yönet →'}
                </Link>
              </li>
            ))}
          </ul>
        )
      )}

    </>
  )
}
