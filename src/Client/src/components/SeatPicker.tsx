import { useEffect, useMemo, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from 'react-oidc-context'
import { ApiError, toApiError } from '../api/http'
import { ordersApi } from '../api/orders'
import { reservationApi } from '../api/reservation'
import { formatPrice } from '../lib/format'
import { useLiveSeats } from '../lib/useLiveSeats'
import type { SeatMap, SeatMapSeat, SeatMapSection, SeatsChanged } from '../types'
import { ErrorAlert } from './ErrorAlert'

// Order'ın kuralıyla aynı (OrderRules:MaxSeatsPerOrder); asıl kontrol backend'de.
const MAX_SEATS = 10

interface Props {
  eventId: string
}

const SEAT_CLASS = { Available: 'seat', Held: 'seat seat--held', Sold: 'seat seat--taken' } as const
const SEAT_LABEL = { Available: '', Held: ', sepette', Sold: ', dolu' } as const

// Anlık değişikliği haritaya uygular; bölümlerin boş koltuk sayısı ve "tükendi" bilgisi yeniden hesaplanır.
function applyChange(map: SeatMap, change: SeatsChanged): SeatMap {
  const changed = new Set(change.seatIds)
  const sections = map.sections.map((section) => {
    if (!section.seats.some((seat) => changed.has(seat.seatId))) return section
    const seats = section.seats.map((seat) =>
      changed.has(seat.seatId) ? { ...seat, state: change.state, isAvailable: change.state === 'Available' } : seat,
    )
    return { ...section, seats, availableCount: seats.filter((seat) => seat.isAvailable).length }
  })
  return { ...map, sections, soldOut: sections.every((section) => section.seats.every((seat) => seat.state === 'Sold')) }
}

// API koltukları sıra ve numaraya göre sıralı verir; burada yalnızca sıralara ayrılır, sıra korunur.
function groupByRow(seats: SeatMapSeat[]) {
  const rows = new Map<string, SeatMapSeat[]>()
  for (const seat of seats) rows.set(seat.row, [...(rows.get(seat.row) ?? []), seat])
  return [...rows.entries()]
}

export function SeatPicker({ eventId }: Props) {
  const auth = useAuth()
  const navigate = useNavigate()
  const [map, setMap] = useState<SeatMap | null>(null)
  const [loadError, setLoadError] = useState<ApiError | null>(null)
  const [selected, setSelected] = useState<string[]>([])
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<ApiError | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  // Anlık güncelleme geri çağrısı en güncel seçimi ve sipariş durumunu görsün.
  const selectedRef = useRef(selected)
  const busyRef = useRef(busy)
  useEffect(() => {
    selectedRef.current = selected
    busyRef.current = busy
  })

  // Başkası seçtiğimiz bir koltuğu aldıysa seçimden çıkar. Sipariş açılırken kendi koltuklarımız da "sepette"ye döner;
  // o sırada dokunulmaz, sayfa sipariş ekranına geçer.
  const live = useLiveSeats(
    eventId,
    (change) => {
      setMap((current) => (current ? applyChange(current, change) : current))
      if (change.state === 'Available' || busyRef.current) return
      const lost = change.seatIds.filter((id) => selectedRef.current.includes(id))
      if (lost.length === 0) return
      setSelected((current) => current.filter((id) => !lost.includes(id)))
      setNotice('Seçtiğiniz koltuklardan biri az önce başkası tarafından alındı; seçiminizden çıkarıldı.')
    },
    () => {
      reservationApi.seats(eventId).then(setMap, () => undefined)
    },
  )

  useEffect(() => {
    let ignore = false
    reservationApi.seats(eventId).then(
      (data) => {
        if (!ignore) setMap(data)
      },
      (err) => {
        if (!ignore) setLoadError(toApiError(err))
      },
    )
    return () => {
      ignore = true
    }
  }, [eventId])

  // Seçilen koltuğun bölümü ve fiyatı: özet ve toplam için.
  const seatInfo = useMemo(() => {
    const info = new Map<string, { section: SeatMapSection; seat: SeatMapSeat }>()
    for (const section of map?.sections ?? []) for (const seat of section.seats) info.set(seat.seatId, { section, seat })
    return info
  }, [map])

  if (loadError?.status === 404) {
    // Etkinlik yeni yayınlandıysa Reservation koltukları birkaç saniye içinde hazırlar.
    return <p className="muted">Koltuklar hazırlanıyor; birazdan sayfayı yenileyin.</p>
  }
  if (loadError) return <ErrorAlert error={loadError} />
  if (!map) return <div className="skeleton skeleton--sm" />

  const total = selected.reduce((sum, id) => sum + (seatInfo.get(id)?.section.price ?? 0), 0)

  const toggle = (seatId: string) => {
    setError(null)
    setNotice(null)
    setSelected((current) =>
      current.includes(seatId)
        ? current.filter((id) => id !== seatId)
        : current.length >= MAX_SEATS
          ? current
          : [...current, seatId],
    )
  }

  const buy = async () => {
    if (!auth.isAuthenticated) {
      // Girişten sonra bu sayfaya döner; seçim kaybolur, yeniden seçilir.
      void auth.signinRedirect({ redirect_uri: window.location.origin + window.location.pathname })
      return
    }
    setBusy(true)
    setError(null)
    try {
      const order = await ordersApi.create(eventId, selected, auth.user!.access_token)
      navigate(`/siparis/${order.orderId}`)
    } catch (err) {
      setError(toApiError(err))
      setBusy(false)
    }
  }

  return (
    <div className="seat-picker">
      {map.soldOut ? (
        <div className="notice notice--soldout">Bu etkinliğin biletleri tükendi.</div>
      ) : (
        !map.isOnSale && <p className="muted">Bu etkinliğin satışı kapalı.</p>
      )}

      <ul className="seat-legend" aria-label="Koltuk durumları">
        <li>
          <span className="seat seat--legend" /> Boş
        </li>
        <li>
          <span className="seat seat--legend" aria-pressed="true" /> Seçili
        </li>
        <li>
          <span className="seat seat--legend seat--held" /> Sepette
        </li>
        <li>
          <span className="seat seat--legend seat--taken" /> Dolu
        </li>
        {live && <li className="seat-legend__live">Anlık güncelleniyor</li>}
      </ul>

      {notice && <div className="notice notice--warning">{notice}</div>}

      <div className="seat-picker__stage" aria-hidden="true">
        SAHNE
      </div>

      {map.sections.map((section) => (
        <section key={section.sectionId} className="seat-section" aria-label={section.sectionName}>
          <div className="seat-section__head">
            <h3 className="seat-section__name">{section.sectionName}</h3>
            <span className="seat-section__price">{formatPrice(section.price)}</span>
            <span className="muted">{section.availableCount} boş koltuk</span>
          </div>
          <div className="seat-rows">
            {groupByRow(section.seats).map(([row, seats]) => (
              <div key={row} className="seat-row">
                <span className="seat-row__label">{row}</span>
                <div className="seat-row__seats">
                  {seats.map((seat) => {
                    const isSelected = selected.includes(seat.seatId)
                    return (
                      <button
                        key={seat.seatId}
                        type="button"
                        className={SEAT_CLASS[seat.state]}
                        aria-pressed={isSelected}
                        aria-label={`${section.sectionName}, sıra ${row}, koltuk ${seat.number}, ${formatPrice(section.price)}${SEAT_LABEL[seat.state]}`}
                        disabled={!map.isOnSale || !seat.isAvailable || (!isSelected && selected.length >= MAX_SEATS)}
                        onClick={() => toggle(seat.seatId)}
                      >
                        {seat.number}
                      </button>
                    )
                  })}
                </div>
              </div>
            ))}
          </div>
        </section>
      ))}

      {error && <ErrorAlert error={error} />}

      {selected.length > 0 && (
        <div className="seat-summary" role="status">
          <div className="seat-summary__info">
            <span>
              {selected.length} koltuk{selected.length >= MAX_SEATS && ` · en fazla ${MAX_SEATS}`}
            </span>
            <strong className="seat-summary__total">{formatPrice(total)}</strong>
          </div>
          <div className="seat-summary__actions">
            <button type="button" className="button button--ghost seat-summary__clear" onClick={() => setSelected([])} disabled={busy}>
              Temizle
            </button>
            <button type="button" className="button button--lg" onClick={() => void buy()} disabled={busy}>
              {busy ? 'Sipariş açılıyor…' : auth.isAuthenticated ? 'Satın al' : 'Giriş yap ve satın al'}
            </button>
          </div>
        </div>
      )}
    </div>
  )
}
