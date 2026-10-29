import type { CreatedOrder, OrderDetail, OrderSummary, PaymentAccepted, PaymentRequest } from '../types'
import { request } from './http'

export const ordersApi = {
  // 202: sipariş açıldı, koltuklar arkada tutuluyor; sonucu get() gösterir.
  create: (eventId: string, seatIds: string[], token: string) =>
    request<CreatedOrder>('/api/orders', { method: 'POST', body: { eventId, seatIds }, token }),

  get: (id: string, token: string) => request<OrderDetail>(`/api/orders/${id}`, { token }),

  mine: (token: string) => request<OrderSummary[]>('/api/orders/mine', { token }),

  // 202: ödeme arkada yapılır; sonucu get() gösterir (lastPaymentId bu denemeyi gösterince).
  pay: (id: string, card: PaymentRequest, token: string) =>
    request<PaymentAccepted>(`/api/orders/${id}/pay`, { method: 'POST', body: card, token }),

  // Yalnızca ödeme beklenirken; koltuklar arkada bırakılır.
  cancel: (id: string, token: string) => request<unknown>(`/api/orders/${id}/cancel`, { method: 'POST', token }),
}
