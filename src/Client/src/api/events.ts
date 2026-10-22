import type { CreatedEvent, CreateEventInput, EventDetail, EventSummary, MyEvent, PriceInput } from '../types'
import { request } from './http'

export const eventsApi = {
  list: () => request<EventSummary[]>('/api/events'),

  // Taslağı yalnızca sahibi ve admin görür; token yoksa taslak için 404 döner.
  get: (id: string, token?: string) => request<EventDetail>(`/api/events/${id}`, { token }),

  mine: (token: string) => request<MyEvent[]>('/api/events/mine', { token }),

  create: (input: CreateEventInput, token: string) =>
    request<CreatedEvent>('/api/events', { method: 'POST', body: input, token }),

  // Ad ve tarih başlamadan önce her zaman, mekân yalnızca taslakta değişir; mekân değişirse fiyatlar silinir.
  update: (id: string, input: CreateEventInput, token: string) =>
    request<unknown>(`/api/events/${id}`, { method: 'PUT', body: input, token }),

  // Yalnızca taslak silinir; yayındaki etkinlik iptal edilir.
  remove: (id: string, token: string) => request<unknown>(`/api/events/${id}`, { method: 'DELETE', token }),

  // Geri alınamaz.
  cancel: (id: string, reason: string, token: string) =>
    request<unknown>(`/api/events/${id}/cancel`, { method: 'POST', body: { reason }, token }),

  // Liste bütün olarak gönderilir: listede olmayan bölümün fiyatı kalkar.
  setPrices: (id: string, prices: PriceInput[], token: string) =>
    request<unknown>(`/api/events/${id}/prices`, { method: 'PUT', body: { prices }, token }),

  publish: (id: string, token: string) => request<unknown>(`/api/events/${id}/publish`, { method: 'POST', token }),
}
