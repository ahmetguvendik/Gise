import type { AuditRecord, PagedResult } from '../types'
import { request } from './http'

export interface AuditFilter {
  action?: string
  outcome?: string
  actorId?: string
  from?: string
  to?: string
}

// Admin: denetim kayıtları (audit-api), yeniden eskiye; sayfada 20 kayıt, page 1'den başlar.
export const auditApi = {
  list: (filter: AuditFilter, page: number, token: string) => {
    const params = new URLSearchParams({ page: String(page) })
    for (const [key, value] of Object.entries(filter)) if (value) params.set(key, value)
    return request<PagedResult<AuditRecord>>(`/api/audit/records?${params}`, { token })
  },
}
