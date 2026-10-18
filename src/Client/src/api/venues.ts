import type { VenueDetail, VenueSectionInput, VenueSummary } from '../types'
import { request } from './http'

export const venuesApi = {
  list: () => request<VenueSummary[]>('/api/venues'),

  // Bölümler ve sıralarıyla.
  get: (id: string) => request<VenueDetail>(`/api/venues/${id}`),

  create: (input: { name: string; city: string; sections: VenueSectionInput[] }, token: string) =>
    request<{ id: string }>('/api/venues', { method: 'POST', body: input, token }),

  // Ad ve şehir her zaman değişebilir.
  update: (id: string, input: { name: string; city: string }, token: string) =>
    request<unknown>(`/api/venues/${id}`, { method: 'PUT', body: input, token }),

  // Bölüm düzeni yalnızca mekânda etkinlik yokken değişir.
  updateLayout: (id: string, sections: VenueSectionInput[], token: string) =>
    request<unknown>(`/api/venues/${id}/layout`, { method: 'PUT', body: { sections }, token }),

  remove: (id: string, token: string) => request<unknown>(`/api/venues/${id}`, { method: 'DELETE', token }),
}
