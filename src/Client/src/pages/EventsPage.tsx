import { useEffect, useState } from 'react'
import { useAuth } from 'react-oidc-context'
import { ApiError } from '../api/http'
import { eventsApi } from '../api/events'
import { canManageEvents } from '../auth/roles'
import { CreateEventForm } from '../components/CreateEventForm'
import { EventList } from '../components/EventList'
import type { EventSummary } from '../types'

export function EventsPage() {
  const auth = useAuth()
  const [events, setEvents] = useState<EventSummary[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [version, setVersion] = useState(0)

  useEffect(() => {
    // Geç gelen eski bir yanıt, yenisinin üzerine yazmasın.
    let ignore = false
    eventsApi.list().then(
      (data) => {
        if (ignore) return
        setEvents(data)
        setError(null)
      },
      (err) => {
        if (!ignore) setError(err instanceof ApiError ? err.message : 'Beklenmeyen bir hata oluştu.')
      },
    )
    return () => {
      ignore = true
    }
  }, [version])

  const token = auth.user?.access_token

  return (
    <main className="container page">
      <h1>Yaklaşan etkinlikler</h1>

      {error && <div className="alert">{error}</div>}
      {events === null && !error ? <p className="muted">Yükleniyor…</p> : events && <EventList events={events} />}

      {token && canManageEvents(auth.user) && (
        <CreateEventForm token={token} onCreated={() => setVersion((v) => v + 1)} />
      )}
    </main>
  )
}
