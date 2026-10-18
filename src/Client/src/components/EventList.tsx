import { Link } from 'react-router-dom'
import { formatDate, formatPrice } from '../lib/format'
import type { EventSummary, VenueSummary } from '../types'

interface Props {
  events: EventSummary[]
  venues: VenueSummary[]
}

export function EventList({ events, venues }: Props) {
  if (events.length === 0) {
    return <p className="muted">Yaklaşan etkinlik yok.</p>
  }

  const venueById = new Map(venues.map((venue) => [venue.id, venue]))

  return (
    <ul className="event-list">
      {events.map((event) => {
        const venue = venueById.get(event.venueId)
        return (
          <li key={event.id} className="event-card">
            <h3 className="event-card__title">
              <Link to={`/etkinlik/${event.id}`}>{event.title}</Link>
            </h3>
            <p className="muted">
              {venue ? `${venue.name}, ${venue.city}` : 'Mekân bilgisi yok'} · {formatDate(event.startsAt)}
            </p>
            {event.minPrice !== null && <p className="event-card__price">{formatPrice(event.minPrice)}'den başlayan fiyatlarla</p>}
          </li>
        )
      })}
    </ul>
  )
}
