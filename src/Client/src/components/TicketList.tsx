import { useState } from 'react'
import type { FormEvent } from 'react'
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
  /** Biletlerim'de: aktarma ve geri alma. İşlem sonrası onChanged listeyi yeniler. */
  allowTransfer?: boolean
  onChanged?: () => void
}

// Biletlerim ve sipariş sayfası. PDF'te QR var; kod kapıda elle girilebilsin diye burada da yazılır.
export function TicketList({ tickets, showEvent = true, allowTransfer = false, onChanged }: Props) {
  const auth = useAuth()
  const token = auth.user?.access_token ?? ''
  const [busy, setBusy] = useState<string | null>(null)
  const [error, setError] = useState<ApiError | null>(null)
  // Aktarım formu açık olan bilet.
  const [transferring, setTransferring] = useState<string | null>(null)
  const [email, setEmail] = useState('')

  const run = async (ticketId: string, action: () => Promise<void>) => {
    setBusy(ticketId)
    setError(null)
    try {
      await action()
    } catch (err) {
      setError(toApiError(err))
    } finally {
      setBusy(null)
    }
  }

  const download = (ticket: TicketItem) =>
    run(ticket.id, async () => {
      const { url } = await ticketsApi.downloadUrl(ticket.id, token)
      // Adres "attachment" ile imzalı: sayfa değişmez, dosya iner.
      window.location.assign(url)
    })

  const openTransfer = (ticket: TicketItem) => {
    setTransferring(ticket.id)
    setEmail('')
    setError(null)
  }

  const sendTransfer = (event: FormEvent<HTMLFormElement>, ticket: TicketItem) => {
    event.preventDefault()
    void run(ticket.id, async () => {
      await ticketsApi.transfer(ticket.id, email.trim(), token)
      setTransferring(null)
      onChanged?.()
    })
  }

  const cancelTransfer = (ticket: TicketItem) => {
    if (!window.confirm('Aktarımı geri almak istiyor musunuz? Bilet sizde kalır.')) return
    void run(ticket.id, async () => {
      await ticketsApi.cancelTransfer(ticket.id, token)
      onChanged?.()
    })
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
              {ticket.status !== 'Transferred' && (
                <p className="ticket-row__code">
                  <span className="muted">Kod</span> {ticket.code}
                </p>
              )}
            </div>
            <div className="ticket-row__side">
              {ticket.status === 'Cancelled' ? (
                <span className="muted">Bu bilet geçersiz.</span>
              ) : ticket.status === 'Transferred' ? (
                <span className="muted ticket-row__note">
                  {ticket.transferredTo} adresine aktarıldı
                  {ticket.transferredAt && ` · ${formatDate(ticket.transferredAt)}`}. Kod ve PDF artık alıcının.
                </span>
              ) : transferring === ticket.id ? (
                <form className="transfer-form" onSubmit={(event) => sendTransfer(event, ticket)}>
                  <label>
                    Alıcının e-postası
                    <input
                      type="email"
                      required
                      maxLength={254}
                      autoComplete="off"
                      value={email}
                      onChange={(event) => setEmail(event.target.value)}
                      autoFocus
                    />
                  </label>
                  <p className="muted ticket-row__note">
                    Alıcı bu adresle giriş yapıp kabul edince bilet ona geçer ve sizdeki QR geçersiz olur. Bilet yalnızca bir kez
                    aktarılabilir. Son an: {formatDate(ticket.transferDeadline)}.
                  </p>
                  <div className="ticket-row__actions">
                    <button type="submit" className="button" disabled={busy === ticket.id}>
                      {busy === ticket.id ? 'Gönderiliyor…' : 'Gönder'}
                    </button>
                    <button type="button" className="button button--ghost" onClick={() => setTransferring(null)}>
                      Vazgeç
                    </button>
                  </div>
                </form>
              ) : (
                <>
                  <div className="ticket-row__actions">
                    <button
                      type="button"
                      className="button button--outline"
                      onClick={() => void download(ticket)}
                      disabled={busy === ticket.id}
                    >
                      PDF indir
                    </button>
                    {allowTransfer && ticket.canTransfer && (
                      <button type="button" className="button button--ghost" onClick={() => openTransfer(ticket)}>
                        Aktar
                      </button>
                    )}
                    {allowTransfer && ticket.pendingTransfer && (
                      <button
                        type="button"
                        className="button button--ghost"
                        onClick={() => cancelTransfer(ticket)}
                        disabled={busy === ticket.id}
                      >
                        Geri al
                      </button>
                    )}
                  </div>
                  {ticket.pendingTransfer && (
                    <span className="muted ticket-row__note">
                      {ticket.pendingTransfer.toEmail} adresine gönderildi, kabul bekleniyor (son an: {formatDate(ticket.transferDeadline)}).
                    </span>
                  )}
                  {ticket.transferredAt && <span className="muted ticket-row__note">Size aktarıldı; yeniden aktarılamaz.</span>}
                </>
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
