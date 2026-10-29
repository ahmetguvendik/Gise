import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from 'react-oidc-context'
import { ApiError, toApiError } from '../api/http'
import { ordersApi } from '../api/orders'
import { ErrorAlert } from '../components/ErrorAlert'
import { OrderStatusBadge } from '../components/OrderStatusBadge'
import { formatDate, formatLongDate, formatPrice, formatTime } from '../lib/format'
import type { OrderSummary } from '../types'

// Guard'ın arkasında: kullanıcı giriş yapmış. En yeni sipariş önce.
export function MyOrdersPage() {
  const auth = useAuth()
  const token = auth.user?.access_token ?? ''
  const [orders, setOrders] = useState<OrderSummary[] | null>(null)
  const [error, setError] = useState<ApiError | null>(null)

  useEffect(() => {
    let ignore = false
    ordersApi.mine(token).then(
      (data) => {
        if (ignore) return
        setOrders(data)
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
      <h1 className="page-title">Siparişlerim</h1>

      {error && <ErrorAlert error={error} />}

      {orders === null && !error ? (
        <div className="skeleton skeleton--sm" />
      ) : orders && orders.length === 0 ? (
        <div className="card empty">
          <p className="muted">Henüz siparişiniz yok.</p>
          <Link className="button" to="/">
            Etkinliklere göz at
          </Link>
        </div>
      ) : (
        orders && (
          <ul className="order-list">
            {orders.map((order) => (
              <li key={order.id} className="order-row">
                <div className="order-row__main">
                  <OrderStatusBadge status={order.status} />
                  <h2 className="order-row__title">
                    <Link to={`/siparis/${order.id}`}>{order.eventTitle || 'Etkinlik'}</Link>
                  </h2>
                  {order.eventTitle && (
                    <p className="muted">
                      {formatLongDate(order.eventStartsAt)} · {formatTime(order.eventStartsAt)} · {order.venueName}, {order.city}
                    </p>
                  )}
                </div>
                <div className="order-row__side">
                  <strong className="order-row__total">{order.seatCount > 0 ? formatPrice(order.totalAmount) : '—'}</strong>
                  <span className="muted">{order.seatCount > 0 ? `${order.seatCount} koltuk` : 'Koltuk yok'}</span>
                  <span className="muted order-row__date">{formatDate(order.createdAt)}</span>
                </div>
              </li>
            ))}
          </ul>
        )
      )}
    </div>
  )
}
