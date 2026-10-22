import type { CreatedEvent, CreateEventInput, EventDetail, EventSummary, MyEvent, PendingEvent, PriceInput } from '../types'
import { request } from './http'

export const eventsApi = {
  list: () => request<EventSummary[]>('/api/events'),

  // Taslağı yalnızca sahibi ve admin görür; token yoksa taslak için 404 döner.
  get: (id: string, token?: string) => request<EventDetail>(`/api/events/${id}`, { token }),

  mine: (token: string) => request<MyEvent[]>('/api/events/mine', { token }),

  // Yalnızca admin: tüm organizatörlerin onaya gönderdikleri.
  pending: (token: string) => request<PendingEvent[]>('/api/events/pending', { token }),

  create: (input: CreateEventInput, token: string) =>
    request<CreatedEvent>('/api/events', { method: 'POST', body: input, token }),

  // Taslağı sahibi ve admin, yayındakini yalnızca admin değiştirir; mekân yalnızca taslakta değişir, değişirse fiyatlar silinir.
  update: (id: string, input: CreateEventInput, token: string) =>
    request<unknown>(`/api/events/${id}`, { method: 'PUT', body: input, token }),

  // Yalnızca taslak silinir; yayındaki etkinlik iptal edilir.
  remove: (id: string, token: string) => request<unknown>(`/api/events/${id}`, { method: 'DELETE', token }),

  // Yalnızca admin. Geri alınamaz.
  cancel: (id: string, reason: string, token: string) =>
    request<unknown>(`/api/events/${id}/cancel`, { method: 'POST', body: { reason }, token }),

  // Anahtar mediaApi.upload'dan gelir; null afişi kaldırır. Bilgi ve fiyatla aynı yetki kuralları.
  setPoster: (id: string, posterKey: string | null, token: string) =>
    request<unknown>(`/api/events/${id}/poster`, { method: 'PUT', body: { posterKey }, token }),

  // Liste bütün olarak gönderilir: listede olmayan bölümün fiyatı kalkar.
  setPrices: (id: string, prices: PriceInput[], token: string) =>
    request<unknown>(`/api/events/${id}/prices`, { method: 'PUT', body: { prices }, token }),

  // Organizatör taslağı onaya gönderir; her bölüm fiyatlı olmalı. Onay beklerken değişiklik yapılamaz.
  submit: (id: string, token: string) => request<unknown>(`/api/events/${id}/submit`, { method: 'POST', token }),

  // Onay bekleyen etkinlik yeniden taslak olur.
  withdraw: (id: string, token: string) => request<unknown>(`/api/events/${id}/withdraw`, { method: 'POST', token }),

  // Yalnızca admin: sebebi organizatör görür.
  returnToDraft: (id: string, reason: string, token: string) =>
    request<unknown>(`/api/events/${id}/return`, { method: 'POST', body: { reason }, token }),

  // Yalnızca admin; taslaktan ya da onay bekleyenden.
  publish: (id: string, token: string) => request<unknown>(`/api/events/${id}/publish`, { method: 'POST', token }),
}
