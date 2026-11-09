import type { ManagedUserPage } from '../types'
import { request } from './http'

// Admin: kullanıcı arama ve rol verme/alma (identity-api). Rol, kullanıcının bir sonraki token'ında (en geç 5 dk) görünür.
export const usersApi = {
  // search boşsa bütün kullanıcılar; 20'şerli, first sayfanın başı.
  search: (search: string, first: number, token: string) =>
    request<ManagedUserPage>(`/api/users?${search ? `search=${encodeURIComponent(search)}&` : ''}first=${first}`, { token }),

  grantRole: (id: string, role: string, token: string) =>
    request<void>(`/api/users/${id}/roles/${encodeURIComponent(role)}`, { method: 'PUT', token }),

  revokeRole: (id: string, role: string, token: string) =>
    request<void>(`/api/users/${id}/roles/${encodeURIComponent(role)}`, { method: 'DELETE', token }),
}
