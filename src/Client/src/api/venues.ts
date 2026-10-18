import type { VenueDetail, VenueSummary } from '../types'
import { request } from './http'

export const venuesApi = {
  list: () => request<VenueSummary[]>('/api/venues'),

  // Bölümler ve sıralarıyla.
  get: (id: string) => request<VenueDetail>(`/api/venues/${id}`),
}
