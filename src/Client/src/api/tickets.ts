import type { CheckInResult, TicketDownload, TicketItem } from '../types'
import { request } from './http'

export const ticketsApi = {
  // orderId verilirse yalnızca o siparişin biletleri.
  mine: (token: string, orderId?: string) =>
    request<TicketItem[]>(`/api/tickets/mine${orderId ? `?orderId=${encodeURIComponent(orderId)}` : ''}`, { token }),

  // Kısa süreli indirme adresi; tarayıcı PDF'i doğrudan MinIO'dan indirir.
  downloadUrl: (id: string, token: string) => request<TicketDownload>(`/api/tickets/${id}/download`, { token }),

  // Kapı görevlisi ve admin. Kullanılmışsa 409, iptal edilmişse 422, kod yoksa 404.
  checkIn: (code: string, token: string) =>
    request<CheckInResult>('/api/tickets/check-in', { method: 'POST', body: { code }, token }),
}
