import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useAuth } from 'react-oidc-context'
import { ApiError, toApiError } from '../api/http'
import { eventsApi } from '../api/events'
import { venuesApi } from '../api/venues'
import { isAdmin } from '../auth/roles'
import { useSessionReady } from '../auth/SilentSignin'
import { CancelEventForm } from '../components/CancelEventForm'
import { ErrorAlert } from '../components/ErrorAlert'
import { EventInfoForm } from '../components/EventInfoForm'
import { Poster } from '../components/Poster'
import { PriceForm } from '../components/PriceForm'
import { StatusBadge } from '../components/StatusBadge'
import { formatDate, formatLongDate, formatPrice, formatTime } from '../lib/format'
import type { EventDetail, VenueDetail } from '../types'

export function EventDetailPage() {
  const { id = '' } = useParams()
  const auth = useAuth()
  const navigate = useNavigate()
  const sessionReady = useSessionReady()
  const token = auth.user?.access_token
  const [event, setEvent] = useState<EventDetail | null>(null)
  const [venue, setVenue] = useState<VenueDetail | null>(null)
  const [loadError, setLoadError] = useState<ApiError | null>(null)
  const [version, setVersion] = useState(0)
  const [busy, setBusy] = useState(false)
  const [actionError, setActionError] = useState<ApiError | null>(null)
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
  const unpriced = venue.sections.filter((s) => !priceBySection.has(s.id))
  const minPrice = event.prices.length > 0 ? Math.min(...event.prices.map((p) => p.price)) : null
  const canManage = !!token && (isAdmin(auth.user) || auth.user?.profile.sub === event.organizerId)
  const cancelled = event.status === 'Cancelled'
  // Başlamış etkinlikte API değişikliği reddeder; formlar hiç gösterilmez.
  const started = new Date(event.startsAt) <= new Date()

  const announce = (message: string) => {
    setNotice(message)
    setVersion((v) => v + 1)
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }

  const publish = async () => {
    setBusy(true)
    setActionError(null)
    try {
      await eventsApi.publish(event.id, token!)
      announce('Etkinlik yayınlandı; artık herkese açık listede görünüyor.')
    } catch (err) {
      setActionError(toApiError(err))
    } finally {
      setBusy(false)
    }
  }

  const remove = async () => {
    if (!window.confirm(`"${event.title}" taslağı fiyatlarıyla birlikte silinsin mi? Bu geri alınamaz.`)) return
    setBusy(true)
    setActionError(null)
    try {
      await eventsApi.remove(event.id, token!)
      navigate('/etkinliklerim')
    } catch (err) {
      setActionError(toApiError(err))
      setBusy(false)
    }
  }

  return (
    <div className="container page">
      <Link className="back-link" to={canManage ? '/etkinliklerim' : '/'}>
        ← {canManage ? 'Etkinliklerim' : 'Etkinlikler'}
      </Link>

      {notice && <div className="notice">{notice}</div>}
      {actionError && <ErrorAlert error={actionError} />}

      {cancelled && (
        <div className="cancel-banner" role="status">
          <strong className="cancel-banner__title">Bu etkinlik iptal edildi</strong>
          {event.cancelledAt && <span>İptal tarihi: {formatDate(event.cancelledAt)}</span>}
          {event.cancellationReason && <p className="cancel-banner__reason">{event.cancellationReason}</p>}
        </div>
      )}

      <section className="detail-hero">
        <Poster id={event.id} title={event.title} startsAt={event.startsAt} size="lg" />
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
        <section className="card card--dashed manage" aria-labelledby="yonetim">
          <div className="manage__head">
            <h2 id="yonetim" className="manage__title">
              Yönetim
            </h2>
            <span className="muted">Yalnızca etkinliğin sahibi ve yönetici görür</span>
          </div>

          {started ? (
            <p className="muted">Etkinlik başladı; artık değişiklik yapılamaz.</p>
          ) : (
            <>
              <EventInfoForm event={event} venueName={`${venue.name}, ${venue.city}`} token={token!} onSaved={() => setVersion((v) => v + 1)} />

              {/* Mekân değişince bölümler değişir: form yeni bölümlerle baştan kurulur. */}
              <PriceForm
                key={venue.id}
                eventId={event.id}
                token={token!}
                sections={venue.sections}
                prices={event.prices}
                onSaved={() => setVersion((v) => v + 1)}
              />

              {event.status === 'Draft' && (
                <div className="manage__block">
                  <h3>Yayınla</h3>
                  <p className="muted">
                    {unpriced.length > 0
                      ? `Yayınlamak için her bölümün fiyatı girilmeli. Eksik: ${unpriced.map((s) => s.name).join(', ')}.`
                      : 'Etkinlik herkese açık listeye düşer ve satışa açılır.'}
                  </p>
                  <button className="button button--lg" onClick={() => void publish()} disabled={busy || unpriced.length > 0}>
                    {busy ? 'Yayınlanıyor…' : 'Yayınla'}
                  </button>
                </div>
              )}

              {event.status === 'Draft' ? (
                <div className="manage__danger">
                  <h3>Taslağı sil</h3>
                  <p className="muted">Taslak henüz kimseye görünmüyor; silinince fiyatlarıyla birlikte kalkar.</p>
                  <button className="button button--danger-outline" onClick={() => void remove()} disabled={busy}>
                    Taslağı sil
                  </button>
                </div>
              ) : (
                <CancelEventForm eventId={event.id} title={event.title} token={token!} onCancelled={() => announce('Etkinlik iptal edildi.')} />
              )}
            </>
          )}
        </section>
      )}
    </div>
  )
}
