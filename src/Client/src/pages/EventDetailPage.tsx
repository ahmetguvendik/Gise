import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useAuth } from 'react-oidc-context'
import { ApiError, toApiError } from '../api/http'
import { eventsApi } from '../api/events'
import { venuesApi } from '../api/venues'
import { isAdmin } from '../auth/roles'
import { useSessionReady } from '../auth/SilentSignin'
import { ErrorAlert } from '../components/ErrorAlert'
import { EventManage } from '../components/EventManage'
import { Poster } from '../components/Poster'
import { StatusBadge } from '../components/StatusBadge'
import { formatDate, formatLongDate, formatPrice, formatTime } from '../lib/format'
import type { EventDetail, VenueDetail } from '../types'

export function EventDetailPage() {
  const { id = '' } = useParams()
  const auth = useAuth()
  const sessionReady = useSessionReady()
  const token = auth.user?.access_token
  const [event, setEvent] = useState<EventDetail | null>(null)
  const [venue, setVenue] = useState<VenueDetail | null>(null)
  const [loadError, setLoadError] = useState<ApiError | null>(null)
  const [version, setVersion] = useState(0)
  const [notice, setNotice] = useState<string | null>(null)

  useEffect(() => {
    // Taslağı yalnızca sahibi görür: sessiz giriş bitmeden istenirse token gitmez ve 404 döner.
    if (!sessionReady) return
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
  }, [id, token, sessionReady, version])

  if (loadError?.status === 404) {
    return (
      <div className="container page">
        <div className="card empty">
          <h1>Etkinlik bulunamadı</h1>
          <p className="muted">Etkinlik kaldırılmış ya da henüz yayınlanmamış olabilir.</p>
          <Link className="button" to="/">
            Etkinliklere dön
          </Link>
        </div>
      </div>
    )
  }
  if (loadError) {
    return (
      <div className="container page">
        <ErrorAlert error={loadError} />
      </div>
    )
  }
  if (!event || !venue) {
    return (
      <div className="container page">
        <div className="skeleton" />
      </div>
    )
  }

  const priceBySection = new Map(event.prices.map((p) => [p.sectionId, p.price]))
  const minPrice = event.prices.length > 0 ? Math.min(...event.prices.map((p) => p.price)) : null
  const admin = isAdmin(auth.user)
  const owner = !!token && auth.user?.profile.sub === event.organizerId
  const canManage = !!token && (admin || owner)
  const cancelled = event.status === 'Cancelled'
  // Geri bağlantısı, kullanıcının buraya büyük olasılıkla geldiği listeye döner.
  const back =
    event.status === 'PendingReview' && admin && !owner
      ? { to: '/onay-bekleyenler', label: 'Onay bekleyenler' }
      : owner
        ? { to: '/etkinliklerim', label: 'Etkinliklerim' }
        : { to: '/', label: 'Etkinlikler' }

  const announce = (message: string) => {
    setNotice(message)
    setVersion((v) => v + 1)
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }

  return (
    <div className="container page">
      <Link className="back-link" to={back.to}>
        ← {back.label}
      </Link>

      {notice && <div className="notice">{notice}</div>}

      {cancelled && (
        <div className="cancel-banner" role="status">
          <strong className="cancel-banner__title">Bu etkinlik iptal edildi</strong>
          {event.cancelledAt && <span>İptal tarihi: {formatDate(event.cancelledAt)}</span>}
          {event.cancellationReason && <p className="cancel-banner__reason">{event.cancellationReason}</p>}
        </div>
      )}

      <section className="detail-hero">
        <Poster
          id={event.id}
          title={event.title}
          startsAt={event.startsAt}
          size="lg"
          imageUrl={event.posterUrl}
          imageSrcset={event.posterSrcset}
        />
        <div className="detail-hero__info">
          {(canManage || event.status !== 'Published') && <StatusBadge status={event.status} />}
          <h1 className="detail-hero__title">{event.title}</h1>
          <ul className="meta-list">
            <li>
              <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                <rect x="3" y="5" width="18" height="16" rx="3" />
                <path d="M16 3v4M8 3v4M3 10h18" />
              </svg>
              {formatLongDate(event.startsAt)}
            </li>
            <li>
              <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                <circle cx="12" cy="12" r="9" />
                <path d="M12 7v5l3 2" />
              </svg>
              {formatTime(event.startsAt)}
            </li>
            <li>
              <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                <path d="M12 21s-7-6.2-7-11.5A7 7 0 0 1 19 9.5C19 14.8 12 21 12 21z" />
                <circle cx="12" cy="9.5" r="2.5" />
              </svg>
              {venue.name}, {venue.city}
            </li>
          </ul>
          {minPrice !== null && !cancelled && (
            <p className="price-lead">
              <strong>{formatPrice(minPrice)}</strong>'den başlayan fiyatlarla
            </p>
          )}
        </div>
      </section>

      {!cancelled && (
        <section className="stack" aria-labelledby="bolumler">
          <h2 id="bolumler" className="section-title">
            Bölümler ve fiyatlar
          </h2>
          <ul className="section-grid">
            {venue.sections.map((section) => {
              const price = priceBySection.get(section.id)
              return (
                <li key={section.id} className="section-card">
                  <div className="section-card__head">
                    <h3 className="section-card__name">{section.name}</h3>
                    <span className="muted">{section.rows.reduce((sum, row) => sum + row.seatCount, 0)} koltuk</span>
                  </div>
                  <div className="perforation" />
                  {price !== undefined ? (
                    <span className="section-card__price">{formatPrice(price)}</span>
                  ) : (
                    <span className="section-card__price section-card__price--empty">Fiyat girilmedi</span>
                  )}
                </li>
              )
            })}
          </ul>
        </section>
      )}

      {canManage && !cancelled && (
        <EventManage
          event={event}
          venue={venue}
          token={token!}
          admin={admin}
          onChanged={announce}
          onSaved={() => setVersion((v) => v + 1)}
        />
      )}
    </div>
  )
}
