import { useEffect, useState } from 'react'
import { useAuth } from 'react-oidc-context'
import { ApiError, toApiError } from '../api/http'
import { eventsApi } from '../api/events'
import { ErrorAlert } from '../components/ErrorAlert'
import { EventCard } from '../components/EventCard'
import type { PendingEvent } from '../types'

// Guard'ın arkasında: yalnızca admin. Organizatörlerin onaya gönderdiği etkinlikler.
export function PendingEventsPage() {
  const auth = useAuth()
  const token = auth.user?.access_token ?? ''
  const [events, setEvents] = useState<PendingEvent[] | null>(null)
  const [error, setError] = useState<ApiError | null>(null)

  useEffect(() => {
    let ignore = false
    eventsApi.pending(token).then(
      (data) => {
        if (ignore) return
        setEvents(data)
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
        <p className="eyebrow">Yönetici</p>
        <h1 className="page-title">Onay bekleyenler</h1>
        <p className="muted">Organizatörlerin onaya gönderdiği etkinlikler. İnceleyip yayınlayın ya da sebebiyle taslağa geri gönderin.</p>
      </div>

      {error && <ErrorAlert error={error} />}

      {events === null && !error ? (
        <ul className="event-grid" aria-label="Yükleniyor">
          <li className="skeleton" />
          <li className="skeleton" />
          <li className="skeleton" />
        </ul>
      ) : events && events.length === 0 ? (
        <p className="muted">Onay bekleyen etkinlik yok.</p>
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
                status={event.status}
                actionLabel="İncele →"
              />
            ))}
          </ul>
        )
      )}
    </div>
  )
}
