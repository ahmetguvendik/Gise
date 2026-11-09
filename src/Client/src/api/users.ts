import type { ManagedUser } from '../types'
import { request } from './http'

// Admin: kullanıcı arama ve rol verme/alma (identity-api). Rol, kullanıcının bir sonraki token'ında (en geç 5 dk) görünür.
export const usersApi = {
  search: (search: string, token: string) =>
    request<ManagedUser[]>(`/api/users?search=${encodeURIComponent(search)}`, { token }),

  grantRole: (id: string, role: string, token: string) =>
    request<void>(`/api/users/${id}/roles/${encodeURIComponent(role)}`, { method: 'PUT', token }),

  revokeRole: (id: string, role: string, token: string) =>
    request<void>(`/api/users/${id}/roles/${encodeURIComponent(role)}`, { method: 'DELETE', token }),
}
