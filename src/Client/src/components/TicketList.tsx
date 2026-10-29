import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from 'react-oidc-context'
import { ApiError, toApiError } from '../api/http'
import { ticketsApi } from '../api/tickets'
import { formatDate, formatLongDate, formatTime } from '../lib/format'
import type { TicketItem } from '../types'
import { ErrorAlert } from './ErrorAlert'
import { TicketStatusBadge } from './TicketStatusBadge'

interface Props {
  tickets: TicketItem[]
  /** Sipariş sayfasında etkinlik zaten başlıkta yazıyor. */
  showEvent?: boolean
}

// Biletlerim ve sipariş sayfası. PDF'te QR var; kod kapıda elle girilebilsin diye burada da yazılır.
export function TicketList({ tickets, showEvent = true }: Props) {
  const auth = useAuth()
  const token = auth.user?.access_token ?? ''
  const [downloading, setDownloading] = useState<string | null>(null)
  const [error, setError] = useState<ApiError | null>(null)

  const download = async (ticket: TicketItem) => {
    setDownloading(ticket.id)
    setError(null)
    try {
      const { url } = await ticketsApi.downloadUrl(ticket.id, token)
      // Adres "attachment" ile imzalı: sayfa değişmez, dosya iner.
      window.location.assign(url)
    } catch (err) {
      setError(toApiError(err))
    } finally {
      setDownloading(null)
    }
  }

  return (
    <>
      {error && <ErrorAlert error={error} />}
      <ul className="ticket-list">
        {tickets.map((ticket) => (
          <li key={ticket.id} className={`ticket-row ticket-row--${ticket.status.toLowerCase()}`}>
            <div className="ticket-row__main">
              <TicketStatusBadge status={ticket.status} />
              {showEvent && (
                <>
                  <h2 className="ticket-row__title">
                    <Link to={`/etkinlik/${ticket.eventId}`}>{ticket.eventTitle}</Link>
                  </h2>
                  <p className="muted">
                    {formatLongDate(ticket.eventStartsAt)} · {formatTime(ticket.eventStartsAt)} · {ticket.venueName}, {ticket.city}
                  </p>
                </>
              )}
              <p className="ticket-row__seat">
                {ticket.sectionName} · Sıra {ticket.row} · Koltuk {ticket.number}
              </p>
              <p className="ticket-row__code">
                <span className="muted">Kod</span> {ticket.code}
              </p>
            </div>
            <div className="ticket-row__side">
              {ticket.status === 'Cancelled' ? (
                <span className="muted">Bu bilet geçersiz.</span>
              ) : (
                <button
                  type="button"
                  className="button button--outline"
                  onClick={() => void download(ticket)}
                  disabled={downloading === ticket.id}
                >
                  {downloading === ticket.id ? 'Hazırlanıyor…' : 'PDF indir'}
                </button>
              )}
              {ticket.status === 'Used' && ticket.checkedInAt && (
                <span className="muted ticket-row__note">Girişte okutuldu: {formatDate(ticket.checkedInAt)}</span>
              )}
            </div>
          </li>
        ))}
      </ul>
    </>
  )
}
