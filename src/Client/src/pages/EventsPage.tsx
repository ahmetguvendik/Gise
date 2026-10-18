import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { toApiError } from '../api/http'
import { eventsApi } from '../api/events'
import { venuesApi } from '../api/venues'
import { EventCard } from '../components/EventCard'
import { Poster } from '../components/Poster'
import { formatPrice, formatWeekdayTime } from '../lib/format'
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

  const venueById = new Map(venues.map((venue) => [venue.id, venue]))
  const venueLine = (event: EventSummary) => {
    const venue = venueById.get(event.venueId)
    return venue ? `${venue.name}, ${venue.city}` : ''
  }
  // Liste tarihe göre sıralı geliyor: en yakın etkinlik öne çıkar.
  const featured = events?.[0]

  return (
    <>
      <section className="hero">
        <div className="container hero__inner">
          <div className="hero__copy">
            <p className="eyebrow">Biletin cebinde</p>
            <h1 className="hero__title">Bu akşam nereye?</h1>
            <p className="hero__lead">Konser, tiyatro, gösteri: yaklaşan etkinlikler tek yerde.</p>
            <a className="button button--lg" href="#etkinlikler">
              Etkinliklere göz at
            </a>
          </div>

          {featured && (
            <article className="featured">
              <Poster id={featured.id} title={featured.title} startsAt={featured.startsAt} />
              <div className="perforation" />
              <div className="featured__body">
                <h2 className="featured__title">
                  <Link to={`/etkinlik/${featured.id}`}>{featured.title}</Link>
                </h2>
                <p className="muted">
                  {venueLine(featured)} · {formatWeekdayTime(featured.startsAt)}
                </p>
                {featured.minPrice !== null && (
                  <p className="price-lead">
                    <strong>{formatPrice(featured.minPrice)}</strong>'den başlayan fiyatlarla
                  </p>
                )}
              </div>
            </article>
          )}
        </div>
      </section>

      <div id="etkinlikler" className="container page">
        <div className="section-head">
          <h2 className="section-title">Yaklaşan etkinlikler</h2>
          <p className="muted">Tarihe göre sıralı</p>
        </div>

        {error && <div className="alert">{error}</div>}

        {events === null && !error ? (
          <ul className="event-grid" aria-label="Yükleniyor">
            <li className="skeleton" />
            <li className="skeleton" />
            <li className="skeleton" />
          </ul>
        ) : events && events.length === 0 ? (
          <div className="card empty">
            <h3>Şu an yaklaşan etkinlik yok</h3>
            <p className="muted">Yeni etkinlikler yayınlandıkça burada görünecek.</p>
          </div>
        ) : (
          events && (
            <ul className="event-grid">
              {events.map((event) => (
                <EventCard
                  key={event.id}
                  id={event.id}
                  title={event.title}
                  startsAt={event.startsAt}
                  venue={venueLine(event)}
                  minPrice={event.minPrice}
                />
              ))}
            </ul>
          )
        )}
      </div>
    </>
  )
}
