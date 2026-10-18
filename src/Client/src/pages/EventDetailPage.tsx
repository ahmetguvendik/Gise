import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useAuth } from 'react-oidc-context'
import { ApiError, toApiError } from '../api/http'
import { eventsApi } from '../api/events'
import { venuesApi } from '../api/venues'
import { isAdmin } from '../auth/roles'
import { ErrorAlert } from '../components/ErrorAlert'
import { PriceForm } from '../components/PriceForm'
import { StatusBadge } from '../components/StatusBadge'
import { formatDate, formatPrice } from '../lib/format'
import type { EventDetail, VenueDetail } from '../types'

export function EventDetailPage() {
  const { id = '' } = useParams()
  const auth = useAuth()
  const token = auth.user?.access_token
  const [event, setEvent] = useState<EventDetail | null>(null)
  const [venue, setVenue] = useState<VenueDetail | null>(null)
  const [loadError, setLoadError] = useState<ApiError | null>(null)
  const [version, setVersion] = useState(0)
  const [publishing, setPublishing] = useState(false)
  const [actionError, setActionError] = useState<ApiError | null>(null)
  const [published, setPublished] = useState(false)

  useEffect(() => {
    // Taslağı yalnızca sahibi görür: oturum yüklenmeden istenirse token gitmez ve 404 döner.
    if (auth.isLoading) return
    let ignore = false
    eventsApi
      .get(id, token)
      .then(async (eventData) => {
        const venueData = await venuesApi.get(eventData.venueId)
        if (ignore) return
        setEvent(eventData)
        setVenue(venueData)
        setLoadError(null)
      })
      .catch((err) => {
        if (!ignore) setLoadError(toApiError(err))
      })
    return () => {
      ignore = true
    }
  }, [id, token, auth.isLoading, version])

  if (loadError?.status === 404) {
    return (
      <div className="card empty">
        <h1>Etkinlik bulunamadı</h1>
        <Link to="/">Etkinliklere dön</Link>
      </div>
    )
  }
  if (loadError) return <ErrorAlert error={loadError} />
  if (!event || !venue) return <p className="muted">Yükleniyor…</p>

  const priceBySection = new Map(event.prices.map((p) => [p.sectionId, p.price]))
  const unpriced = venue.sections.filter((s) => !priceBySection.has(s.id))
  const canManage = !!token && (isAdmin(auth.user) || auth.user?.profile.sub === event.organizerId)

  const publish = async () => {
    setPublishing(true)
    setActionError(null)
    try {
      await eventsApi.publish(event.id, token!)
      setPublished(true)
      setVersion((v) => v + 1)
      window.scrollTo({ top: 0, behavior: 'smooth' })
    } catch (err) {
      setActionError(toApiError(err))
    } finally {
      setPublishing(false)
    }
  }

  return (
    <>
      <div className="event-card__head">
        <h1>{event.title}</h1>
        <StatusBadge status={event.status} />
      </div>
      <p className="muted">
        {venue.name}, {venue.city} · {formatDate(event.startsAt)}
      </p>

      {published && <div className="notice">Etkinlik yayınlandı; artık herkese açık listede görünüyor.</div>}
      {actionError && <ErrorAlert error={actionError} />}

      <section className="card">
        <h2>Bölümler</h2>
        <table className="table">
          <thead>
            <tr>
              <th>Bölüm</th>
              <th>Koltuk</th>
              <th>Fiyat</th>
            </tr>
          </thead>
          <tbody>
            {venue.sections.map((section) => {
              const price = priceBySection.get(section.id)
              return (
                <tr key={section.id}>
                  <td>{section.name}</td>
                  <td>{section.rows.reduce((sum, row) => sum + row.seatCount, 0)}</td>
                  <td>{price !== undefined ? formatPrice(price) : <span className="muted">Girilmedi</span>}</td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </section>

      {canManage && event.status !== 'Cancelled' && (
        <PriceForm
          eventId={event.id}
          token={token!}
          sections={venue.sections}
          prices={event.prices}
          onSaved={() => setVersion((v) => v + 1)}
        />
      )}

      {canManage && event.status === 'Draft' && (
        <section className="card form">
          <h2>Yayınla</h2>
          <p className="muted">
            {unpriced.length > 0
              ? `Yayınlamak için her bölümün fiyatı girilmeli. Eksik: ${unpriced.map((s) => s.name).join(', ')}.`
              : 'Etkinlik herkese açık listeye düşer ve satışa açılır.'}
          </p>
          <button className="button" onClick={() => void publish()} disabled={publishing || unpriced.length > 0}>
            {publishing ? 'Yayınlanıyor…' : 'Yayınla'}
          </button>
        </section>
      )}
    </>
  )
}
