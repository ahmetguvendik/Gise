import type { AcceptedTransfer, CheckInResult, IncomingTransfer, OfferedTransfer, TicketDownload, TicketItem } from '../types'
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

  // Bileti bir e-posta adresine aktarma teklifi; alıcı o adresle giriş yapıp kabul edince bilet ona geçer.
  transfer: (id: string, email: string, token: string) =>
    request<OfferedTransfer>(`/api/tickets/${id}/transfer`, { method: 'POST', body: { email }, token }),

  // Alıcı kabul etmeden önce geri alınabilir.
  cancelTransfer: (id: string, token: string) =>
    request<unknown>(`/api/tickets/${id}/transfer/cancel`, { method: 'POST', token }),

  // Giriş yapanın doğrulanmış e-postasına gönderilmiş biletler.
  incomingTransfers: (token: string) => request<IncomingTransfer[]>('/api/tickets/transfers/incoming', { token }),

  // Bilet giriş yapana geçer; yeni kod ve PDF verilir.
  acceptTransfer: (transferId: string, token: string) =>
    request<AcceptedTransfer>(`/api/tickets/transfers/${transferId}/accept`, { method: 'POST', token }),

  declineTransfer: (transferId: string, token: string) =>
    request<unknown>(`/api/tickets/transfers/${transferId}/decline`, { method: 'POST', token }),
}
