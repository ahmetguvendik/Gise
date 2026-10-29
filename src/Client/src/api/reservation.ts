import type { SeatMap } from '../types'
import { request } from './http'

export const reservationApi = {
  // Herkese açık. Stok henüz kurulmadıysa (etkinlik yeni yayınlandı) 404 STOCK_NOT_FOUND döner.
  seats: (eventId: string) => request<SeatMap>(`/api/reservation/events/${eventId}/seats`),
}
