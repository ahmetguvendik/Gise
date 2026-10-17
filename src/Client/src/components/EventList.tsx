import type { EventSummary } from '../types'

const dateFormatter = new Intl.DateTimeFormat('tr-TR', { dateStyle: 'long', timeStyle: 'short' })

export function EventList({ events }: { events: EventSummary[] }) {
  if (events.length === 0) {
    return <p className="muted">Yaklaşan etkinlik yok.</p>
  }

  return (
    <ul className="event-list">
      {events.map((event) => (
        <li key={event.id} className="event-card">
          <h3 className="event-card__title">{event.title}</h3>
          <p className="muted">
            {event.venue} · {dateFormatter.format(new Date(event.startsAt))}
          </p>
        </li>
      ))}
    </ul>
  )
}
