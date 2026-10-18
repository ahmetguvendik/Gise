import type { EventSummary, VenueSummary } from '../types'

const dateFormatter = new Intl.DateTimeFormat('tr-TR', { dateStyle: 'long', timeStyle: 'short' })
const priceFormatter = new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY', maximumFractionDigits: 2 })

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
            <h3 className="event-card__title">{event.title}</h3>
            <p className="muted">
              {venue ? `${venue.name}, ${venue.city}` : 'Mekân bilgisi yok'} · {dateFormatter.format(new Date(event.startsAt))}
            </p>
            {event.minPrice !== null && <p>{priceFormatter.format(event.minPrice)}'den başlayan fiyatlarla</p>}
          </li>
        )
      })}
    </ul>
  )
}
