import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useAuth } from 'react-oidc-context'
import { ApiError, toApiError } from '../api/http'
import { ordersApi } from '../api/orders'
import { ErrorAlert } from '../components/ErrorAlert'
import { OrderStatusBadge } from '../components/OrderStatusBadge'
import { PaymentForm } from '../components/PaymentForm'
import { formatDate, formatLongDate, formatPrice, formatTime } from '../lib/format'
import type { OrderDetail, OrderStatus, PaymentRequest } from '../types'

// Koltuklar genelde 1–2 saniyede tutulur; o sırada sipariş sık yoklanır.
const PENDING_POLL_MS = 1000
// Ödeme genelde birkaç saniyede sonuçlanır; sağlayıcı cevap vermezse süre dolana kadar sürer.
const PAYMENT_POLL_MS = 2000

// Saga denemeyi bu sürede işlemezse (ör. iki sekmeden aynı anda ödendi, ikincisi yok sayıldı) bekleme bırakılır.
const SAGA_WAIT_MS = 20000

const CLOSED: OrderStatus[] = ['Completed', 'Cancelled', 'Expired']

function formatRemaining(ms: number) {
  const totalSeconds = Math.max(0, Math.ceil(ms / 1000))
  const minutes = Math.floor(totalSeconds / 60)
  const seconds = totalSeconds % 60
  return `${minutes}:${String(seconds).padStart(2, '0')}`
}

// Guard'ın arkasında: kullanıcı giriş yapmış. Siparişi yalnızca sahibi ve admin görür.
export function OrderPage() {
  const { id = '' } = useParams()
  const auth = useAuth()
  const token = auth.user?.access_token ?? ''
  const [order, setOrder] = useState<OrderDetail | null>(null)
  const [loadError, setLoadError] = useState<ApiError | null>(null)
  const [version, setVersion] = useState(0)
  const [now, setNow] = useState(() => Date.now())
  const [cancelling, setCancelling] = useState(false)
  const [actionError, setActionError] = useState<ApiError | null>(null)
  // Gönderilen ödeme denemesi; saga onu işleyene kadar sipariş "ödeme işleniyor" gösterilir.
  const [paying, setPaying] = useState<{ paymentId: string; since: number } | null>(null)

  useEffect(() => {
    let ignore = false
    ordersApi.get(id, token).then(
      (data) => {
        if (ignore) return
        setOrder(data)
        setLoadError(null)
      },
      (err) => {
        if (!ignore) setLoadError(toApiError(err))
      },
    )
    return () => {
      ignore = true
    }
  }, [id, token, version])

  const status = order?.status

  // Gönderilen deneme saga'ya henüz ulaşmadı: sipariş başka bir denemeyi (ya da hiçbirini) gösteriyor ve açık.
  // Sonuçlanınca (reddedildiyse sipariş yeniden "ödeme bekleniyor", sipariş bu denemeyi gösterir) bekleme biter.
  const awaitingSaga =
    paying !== null &&
    order !== null &&
    order.lastPaymentId !== paying.paymentId &&
    !CLOSED.includes(order.status) &&
    now - paying.since < SAGA_WAIT_MS
  const processing = status === 'PaymentProcessing' || awaitingSaga

  // Koltuklar tutulurken, ödeme ya da vazgeçme işlenirken sonucu beklemek için yoklanır.
  useEffect(() => {
    const delay = status === 'Pending' || cancelling || awaitingSaga
      ? PENDING_POLL_MS
      : status === 'PaymentProcessing'
        ? PAYMENT_POLL_MS
        : null
    if (delay === null) return
    const timer = window.setTimeout(() => setVersion((v) => v + 1), delay)
    return () => window.clearTimeout(timer)
  }, [status, cancelling, awaitingSaga, version])

  // Geri sayım; süre dolunca sipariş yeniden okunur (saga onu "Süresi doldu" yapar).
  useEffect(() => {
    if (status !== 'AwaitingPayment' && status !== 'PaymentProcessing') return
    const timer = window.setInterval(() => setNow(Date.now()), 1000)
    return () => window.clearInterval(timer)
  }, [status])

  const remainingMs = order ? new Date(order.expiresAt).getTime() - now : 0
  useEffect(() => {
    if (status === 'AwaitingPayment' && remainingMs <= 0) {
      const timer = window.setTimeout(() => setVersion((v) => v + 1), 2000)
      return () => window.clearTimeout(timer)
    }
  }, [status, remainingMs])

  // Vazgeçme isteği kabul edildikten sonra sipariş kapanınca yoklama durur.
  useEffect(() => {
    if (cancelling && status && status !== 'AwaitingPayment') setCancelling(false)
  }, [cancelling, status])

  if (loadError) {
    return (
      <div className="container page">
        <ErrorAlert error={loadError} />
        <Link className="button button--outline" to="/siparislerim">
          Siparişlerime dön
        </Link>
      </div>
    )
  }
  if (!order) {
    return (
      <div className="container page">
        <div className="skeleton" />
      </div>
    )
  }

  const cancel = async () => {
    if (!window.confirm('Siparişten vazgeçilsin mi? Koltuklarınız başkalarına açılır.')) return
    setCancelling(true)
    setActionError(null)
    try {
      await ordersApi.cancel(order.id, token)
      setVersion((v) => v + 1)
    } catch (err) {
      setActionError(toApiError(err))
      setCancelling(false)
    }
  }

  const pay = async (card: PaymentRequest) => {
    const accepted = await ordersApi.pay(order.id, card, token)
    setActionError(null)
    setPaying({ paymentId: accepted.paymentId, since: Date.now() })
    setVersion((v) => v + 1)
  }

  const awaiting = order.status === 'AwaitingPayment' && !processing
  const closed = order.status === 'Cancelled' || order.status === 'Expired'

  return (
    <div className="container page">
      <Link className="back-link" to="/siparislerim">
        ← Siparişlerim
      </Link>

      <div className="stack">
        <OrderStatusBadge status={order.status} />
        <h1 className="page-title">{order.eventTitle || 'Sipariş'}</h1>
        {order.eventTitle && (
          <p className="muted">
            {formatLongDate(order.eventStartsAt)} · {formatTime(order.eventStartsAt)} · {order.venueName}, {order.city}
          </p>
        )}
      </div>

      {actionError && <ErrorAlert error={actionError} />}

      {order.status === 'Pending' && (
        <div className="order-status order-status--pending" role="status">
          <strong className="order-status__title">Koltuklarınız tutuluyor…</strong>
          <span>Bu birkaç saniye sürer; sayfa kendiliğinden güncellenir.</span>
        </div>
      )}

      {awaiting && order.failureReason && (
        <div className="alert" role="alert">
          <strong>Ödeme alınamadı</strong>
          <div>{order.failureReason} Süre dolmadan başka bir kartla yeniden deneyebilirsiniz.</div>
        </div>
      )}

      {awaiting && (
        <div className="order-status order-status--awaiting" role="status">
          <strong className="order-status__title">Koltuklarınız sizin için ayrıldı</strong>
          <span>Ödeme için kalan süre</span>
          <span className="countdown" aria-live="off">
            {formatRemaining(remainingMs)}
          </span>
          <span>Süre dolarsa koltuklar başkalarına açılır.</span>
        </div>
      )}

      {processing && (
        <div className="order-status order-status--pending" role="status">
          <strong className="order-status__title">Ödemeniz işleniyor…</strong>
          <span>Bu genelde birkaç saniye sürer; sayfa kendiliğinden güncellenir.</span>
          <span>
            Kalan süre {formatRemaining(remainingMs)}. Süre dolarsa sipariş kapanır, çekilen bir tutar varsa iade edilir.
          </span>
        </div>
      )}

      {order.status === 'Completed' && (
        <div className="order-status order-status--done" role="status">
          <strong className="order-status__title">Siparişiniz tamamlandı</strong>
          <span>Ödemeniz alındı, koltuklar sizin.</span>
        </div>
      )}

      {closed && (
        <div className="order-status order-status--closed" role="status">
          <strong className="order-status__title">
            {order.status === 'Expired' ? 'Siparişin süresi doldu' : 'Sipariş iptal edildi'}
          </strong>
          {order.failureReason && <span>{order.failureReason}</span>}
          <Link className="button button--contrast order-status__action" to={`/etkinlik/${order.eventId}`}>
            Koltuk seçimine dön
          </Link>
        </div>
      )}

      {order.items.length > 0 && (
        <section className="card order-items-card" aria-labelledby="koltuklar">
          <h2 id="koltuklar" className="form__title">
            Koltuklar
          </h2>
          <ul className="order-items">
            {order.items.map((item) => (
              <li key={item.seatId} className="order-item">
                <span>
                  {item.sectionName} · Sıra {item.row} · Koltuk {item.number}
                </span>
                <strong>{formatPrice(item.price)}</strong>
              </li>
            ))}
          </ul>
          <div className="order-total">
            <span>Toplam</span>
            <strong>{formatPrice(order.totalAmount)}</strong>
          </div>
        </section>
      )}

      {awaiting && (
        <>
          <section className="card">
            <PaymentForm amount={order.totalAmount} onSubmit={pay} />
          </section>
          <div className="order-actions">
            <button type="button" className="button button--danger-outline" onClick={() => void cancel()} disabled={cancelling}>
              {cancelling ? 'Vazgeçiliyor…' : 'Siparişten vazgeç'}
            </button>
          </div>
        </>
      )}

      <p className="muted">Sipariş tarihi: {formatDate(order.createdAt)}</p>
    </div>
  )
}
