import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from 'react-oidc-context'
import { ApiError, toApiError } from '../api/http'
import { ticketsApi } from '../api/tickets'
import { formatDate, formatLongDate, formatTime } from '../lib/format'
import type { IncomingTransfer } from '../types'
import { ErrorAlert } from './ErrorAlert'

interface Props {
  transfers: IncomingTransfer[]
  /** Kabul ya da ret sonrası listeler yenilenir; kabul edilen bilet Biletlerim'e geçer. */
  onChanged: () => void
}

// Biletlerim'in üstünde: giriş yapanın e-postasına gönderilmiş, kabul bekleyen biletler.
export function IncomingTransfers({ transfers, onChanged }: Props) {
  const auth = useAuth()
  const token = auth.user?.access_token ?? ''
  const [busy, setBusy] = useState<string | null>(null)
  const [error, setError] = useState<ApiError | null>(null)

  const respond = async (transfer: IncomingTransfer, accept: boolean) => {
    if (!accept && !window.confirm('Bileti reddetmek istiyor musunuz? Bilet gönderende kalır.')) return
    setBusy(transfer.transferId)
    setError(null)
    try {
      if (accept) await ticketsApi.acceptTransfer(transfer.transferId, token)
      else await ticketsApi.declineTransfer(transfer.transferId, token)
      onChanged()
    } catch (err) {
      setError(toApiError(err))
    } finally {
      setBusy(null)
    }
  }

  return (
    <section className="stack" aria-labelledby="incoming-transfers">
      <h2 id="incoming-transfers" className="section-title">
        Size gönderilen biletler
      </h2>
      {error && <ErrorAlert error={error} />}
      <ul className="ticket-list">
        {transfers.map((transfer) => (
          <li key={transfer.transferId} className="ticket-row ticket-row--incoming">
            <div className="ticket-row__main">
              <span className="badge badge--ticket-incoming">{transfer.fromName || 'Bir Gişe kullanıcısı'} gönderdi</span>
              <h3 className="ticket-row__title">
                <Link to={`/etkinlik/${transfer.eventId}`}>{transfer.eventTitle}</Link>
              </h3>
              <p className="muted">
                {formatLongDate(transfer.eventStartsAt)} · {formatTime(transfer.eventStartsAt)} · {transfer.venueName}, {transfer.city}
              </p>
              <p className="ticket-row__seat">
                {transfer.sectionName} · Sıra {transfer.row} · Koltuk {transfer.number}
              </p>
            </div>
            <div className="ticket-row__side">
              <div className="ticket-row__actions">
                <button
                  type="button"
                  className="button"
                  onClick={() => void respond(transfer, true)}
                  disabled={busy === transfer.transferId}
                >
                  Kabul et
                </button>
                <button
                  type="button"
                  className="button button--ghost"
                  onClick={() => void respond(transfer, false)}
                  disabled={busy === transfer.transferId}
                >
                  Reddet
                </button>
              </div>
              <span className="muted ticket-row__note">Son an: {formatDate(transfer.expiresAt)}</span>
            </div>
          </li>
        ))}
      </ul>
    </section>
  )
}
