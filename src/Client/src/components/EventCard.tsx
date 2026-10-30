import { Link } from 'react-router-dom'
import { formatPrice, formatWeekdayTime } from '../lib/format'
import type { EventStatus } from '../types'
import { Poster } from './Poster'
import { StatusBadge } from './StatusBadge'

interface Props {
  id: string
  title: string
  startsAt: string
  venue: string
  minPrice: number | null
  posterUrl: string | null
  posterSrcset: string | null
  /** Etkinliklerim'de: durum rozeti ve yönetim düğmesi. */
  status?: EventStatus
  actionLabel?: string
  /** Ana sayfada: bütün koltuklar satılmış ya da az kalmış (Search'ten). */
  soldOut?: boolean
  remainingSeats?: number | null
}

export function EventCard({ id, title, startsAt, venue, minPrice, posterUrl, posterSrcset, status, actionLabel, soldOut = false, remainingSeats }: Props) {
  const to = `/etkinlik/${id}`

  return (
    <li className={`event-card${soldOut ? ' event-card--soldout' : ''}`}>
      <Poster id={id} title={title} startsAt={startsAt} imageUrl={posterUrl} imageSrcset={posterSrcset} />
      <div className="event-card__body">
        {status && (
          <div>
            <StatusBadge status={status} />
          </div>
        )}
        <h3 className="event-card__title">
          <Link to={to}>{title}</Link>
        </h3>
        <p className="event-card__meta">{venue}</p>
        {!soldOut && (remainingSeats ?? 0) > 0 && <span className="badge badge--few-left">Son {remainingSeats} bilet</span>}
        <div className="event-card__footer">
          {soldOut ? (
            <span className="badge badge--soldout">Tükendi</span>
          ) : (
            <span className="price">{minPrice !== null ? `${formatPrice(minPrice)}'den` : 'Fiyat girilmedi'}</span>
          )}
          <span className="muted">{formatWeekdayTime(startsAt)}</span>
        </div>
        {actionLabel && (
          <Link className="button button--contrast event-card__action" to={to}>
            {actionLabel}
          </Link>
        )}
      </div>
    </li>
  )
}
