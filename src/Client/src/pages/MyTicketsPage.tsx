import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from 'react-oidc-context'
import { ApiError, toApiError } from '../api/http'
import { ticketsApi } from '../api/tickets'
import { ErrorAlert } from '../components/ErrorAlert'
import { IncomingTransfers } from '../components/IncomingTransfers'
import { TicketList } from '../components/TicketList'
import type { IncomingTransfer, TicketItem } from '../types'

// Guard'ın arkasında: kullanıcı giriş yapmış. Üstte e-postasına gönderilmiş biletler, altta kendi biletleri (en yakın
// etkinlik önce).
export function MyTicketsPage() {
  const auth = useAuth()
  const token = auth.user?.access_token ?? ''
  const [tickets, setTickets] = useState<TicketItem[] | null>(null)
  const [incoming, setIncoming] = useState<IncomingTransfer[]>([])
  const [error, setError] = useState<ApiError | null>(null)
  // Aktarım, kabul ya da ret sonrası iki liste birlikte yenilenir.
  const [version, setVersion] = useState(0)

  useEffect(() => {
    let ignore = false
    Promise.all([ticketsApi.mine(token), ticketsApi.incomingTransfers(token)]).then(
      ([ticketData, incomingData]) => {
        if (ignore) return
        setTickets(ticketData)
        setIncoming(incomingData)
        setError(null)
      },
      (err) => {
        if (!ignore) setError(toApiError(err))
      },
    )
    return () => {
      ignore = true
    }
  }, [token, version])

  const reload = () => setVersion((current) => current + 1)

  return (
    <div className="container page">
      <h1 className="page-title">Biletlerim</h1>

      {error && <ErrorAlert error={error} />}

      {incoming.length > 0 && <IncomingTransfers transfers={incoming} onChanged={reload} />}

      {tickets === null && !error ? (
        <div className="skeleton skeleton--sm" />
      ) : tickets && tickets.length === 0 ? (
        <div className="card empty">
          <p className="muted">Henüz biletiniz yok.</p>
          <Link className="button" to="/">
            Etkinliklere göz at
          </Link>
        </div>
      ) : (
        tickets && <TicketList tickets={tickets} allowTransfer onChanged={reload} />
      )}
    </div>
  )
}
