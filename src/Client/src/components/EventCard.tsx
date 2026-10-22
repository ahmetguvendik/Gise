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
}

export function EventCard({ id, title, startsAt, venue, minPrice, posterUrl, posterSrcset, status, actionLabel }: Props) {
  const to = `/etkinlik/${id}`

  return (
    <li className="event-card">
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
        <div className="event-card__footer">
          <span className="price">{minPrice !== null ? `${formatPrice(minPrice)}'den` : 'Fiyat girilmedi'}</span>
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
