import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from 'react-oidc-context'
import { ApiError, toApiError } from '../api/http'
import { ticketsApi } from '../api/tickets'
import type { TicketItem } from '../types'
import { ErrorAlert } from './ErrorAlert'
import { TicketList } from './TicketList'

// Tamamlanan siparişin biletleri. Sipariş biletler çıktıktan sonra tamamlandığı için burada hazırdırlar.
export function OrderTickets({ orderId }: { orderId: string }) {
  const auth = useAuth()
  const token = auth.user?.access_token ?? ''
  const [tickets, setTickets] = useState<TicketItem[] | null>(null)
  const [error, setError] = useState<ApiError | null>(null)

  useEffect(() => {
    let ignore = false
    ticketsApi.mine(token, orderId).then(
      (data) => {
        if (!ignore) setTickets(data)
      },
      (err) => {
        if (!ignore) setError(toApiError(err))
      },
    )
    return () => {
      ignore = true
    }
  }, [orderId, token])

  return (
    <section className="stack" aria-labelledby="biletler">
      <div className="section-head">
        <h2 id="biletler" className="form__title">
          Biletler
        </h2>
        <Link to="/biletlerim">Tüm biletlerim</Link>
      </div>
      {error && <ErrorAlert error={error} />}
      {tickets === null && !error ? <div className="skeleton skeleton--sm" /> : tickets && <TicketList tickets={tickets} showEvent={false} />}
    </section>
  )
}
