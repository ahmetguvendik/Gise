import type { User } from 'oidc-client-ts'

// Keycloak, realm rollerini token'daki düz "roles" claim'ine yazıyor (realm'deki protocol mapper).
export function rolesOf(user: User | null | undefined): string[] {
  const roles = user?.profile.roles
  return Array.isArray(roles) ? roles.filter((role): role is string => typeof role === 'string') : []
}

export const canManageEvents = (user: User | null | undefined) =>
  rolesOf(user).some((role) => role === 'organizer' || role === 'admin')
