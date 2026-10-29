import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from 'react-oidc-context'
import { ApiError, toApiError } from '../api/http'
import { ordersApi } from '../api/orders'
import { reservationApi } from '../api/reservation'
import { formatPrice } from '../lib/format'
import type { SeatMap, SeatMapSeat, SeatMapSection } from '../types'
import { ErrorAlert } from './ErrorAlert'

// Order'ın kuralıyla aynı (OrderRules:MaxSeatsPerOrder); asıl kontrol backend'de.
const MAX_SEATS = 10

interface Props {
  eventId: string
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
      {!map.isOnSale && <p className="muted">Bu etkinliğin satışı kapalı.</p>}

      <ul className="seat-legend" aria-label="Koltuk durumları">
        <li>
          <span className="seat seat--legend" /> Boş
        </li>
        <li>
          <span className="seat seat--legend" aria-pressed="true" /> Seçili
        </li>
        <li>
          <span className="seat seat--legend seat--taken" /> Dolu
        </li>
      </ul>

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
                        className={seat.isAvailable ? 'seat' : 'seat seat--taken'}
                        aria-pressed={isSelected}
                        aria-label={`${section.sectionName}, sıra ${row}, koltuk ${seat.number}, ${formatPrice(section.price)}${seat.isAvailable ? '' : ', dolu'}`}
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
