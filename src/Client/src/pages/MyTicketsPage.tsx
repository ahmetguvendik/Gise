import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from 'react-oidc-context'
import { ApiError, toApiError } from '../api/http'
import { ticketsApi } from '../api/tickets'
import { ErrorAlert } from '../components/ErrorAlert'
import { TicketList } from '../components/TicketList'
import type { TicketItem } from '../types'

// Guard'ın arkasında: kullanıcı giriş yapmış. En yakın etkinlik önce.
export function MyTicketsPage() {
  const auth = useAuth()
  const token = auth.user?.access_token ?? ''
  const [tickets, setTickets] = useState<TicketItem[] | null>(null)
  const [error, setError] = useState<ApiError | null>(null)

  useEffect(() => {
    let ignore = false
    ticketsApi.mine(token).then(
      (data) => {
        if (ignore) return
        setTickets(data)
        setError(null)
      },
      (err) => {
        if (!ignore) setError(toApiError(err))
      },
    )
    return () => {
      ignore = true
    }
  }, [token])

  return (
    <div className="container page">
      <h1 className="page-title">Biletlerim</h1>

      {error && <ErrorAlert error={error} />}

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
        tickets && <TicketList tickets={tickets} />
      )}
    </div>
  )
}
