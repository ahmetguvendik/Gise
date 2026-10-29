import type { User } from 'oidc-client-ts'

export const ROLE_LABELS: Record<string, string> = {
  customer: 'Müşteri',
  organizer: 'Organizatör',
  'gate-staff': 'Kapı görevlisi',
  admin: 'Yönetici',
}

// Keycloak, realm rollerini token'daki düz "roles" claim'ine yazıyor (realm'deki protocol mapper).
export function rolesOf(user: User | null | undefined): string[] {
  const roles = user?.profile.roles
  return Array.isArray(roles) ? roles.filter((role): role is string => typeof role === 'string') : []
}

export const hasAnyRole = (user: User | null | undefined, roles: string[]) =>
  rolesOf(user).some((role) => roles.includes(role))

export const isAdmin = (user: User | null | undefined) => hasAnyRole(user, ['admin'])

export const canManageEvents = (user: User | null | undefined) => hasAnyRole(user, ['organizer', 'admin'])

export const canCheckIn = (user: User | null | undefined) => hasAnyRole(user, ['gate-staff', 'admin'])
