import type { VenueSummary } from '../types'
import { request } from './http'

export const venuesApi = {
  list: () => request<VenueSummary[]>('/api/venues'),
}
