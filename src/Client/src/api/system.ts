import type { SystemHealth } from '../types'
import { request } from './http'

export const systemApi = {
  // Admin: servislerin ve altyapının son yoklama sonuçları (healthcheck-api).
  health: (token: string) => request<SystemHealth>('/api/system/health', { token }),
}
