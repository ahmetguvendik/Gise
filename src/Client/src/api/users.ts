import type { ManagedUser, PagedResult } from '../types'
import { request } from './http'

// Admin: kullanıcı listesi ve rol verme/alma (identity-api). Rol, kullanıcının bir sonraki token'ında (en geç 5 dk) görünür.
export const usersApi = {
  // search boşsa bütün kullanıcılar; sayfada 10 kullanıcı (identity-api varsayılanı), page 1'den başlar.
  list: (search: string, page: number, token: string) =>
    request<PagedResult<ManagedUser>>(`/api/users?${search ? `search=${encodeURIComponent(search)}&` : ''}page=${page}`, { token }),

  grantRole: (id: string, role: string, token: string) =>
    request<void>(`/api/users/${id}/roles/${encodeURIComponent(role)}`, { method: 'PUT', token }),

  revokeRole: (id: string, role: string, token: string) =>
    request<void>(`/api/users/${id}/roles/${encodeURIComponent(role)}`, { method: 'DELETE', token }),
}
