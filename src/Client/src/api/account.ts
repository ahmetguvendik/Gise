import { accountUrl } from '../auth/oidcConfig'
import type { AccountProfile, AccountSession, OtpDevice } from '../types'
import { ApiError } from './http'

// Keycloak'ın hesap API'si (/auth/realms/gise/account). Giriş token'ı bu API için de geçerli (aud "account",
// manage-account rolü); istek kullanıcının kendi hesabına gider. Hata gövdesi ProblemDetails değil, Keycloak'ın
// kendi biçimi: { field, errorMessage, params, errors: [...] }.

interface KeycloakFieldError {
  field?: string
  errorMessage?: string
}

interface KeycloakError extends KeycloakFieldError {
  errors?: KeycloakFieldError[]
}

// Keycloak oturumları cihaza göre gruplar; tarayıcı oturumda, işletim sistemi cihazda.
interface DeviceRepresentation {
  os?: string
  osVersion?: string
  sessions: {
    id: string
    ipAddress?: string
    started: number
    lastAccess: number
    expires: number
    browser?: string
    current?: boolean
  }[]
}

interface CredentialType {
  type: string
  userCredentialMetadatas?: { credential: { id: string; userLabel?: string; createdDate?: number } }[]
}

const FIELD_LABELS: Record<string, string> = { firstName: 'Ad', lastName: 'Soyad', email: 'E-posta' }

function fieldMessage({ field = '', errorMessage }: KeycloakFieldError) {
  const label = FIELD_LABELS[field] ?? 'Bu alan'
  switch (errorMessage) {
    case 'error-user-attribute-required':
      return `${label} boş olamaz.`
    case 'error-invalid-length':
      return `${label} en fazla 255 karakter olabilir.`
    case 'error-person-name-invalid-character':
      return `${label} geçersiz karakter içeriyor.`
    default:
      return `${label} geçersiz.`
  }
}

// Keycloak hesap API'si Accept: application/json olmadan bazı uçlarda 405 döner (ör. oturum kapatma).
async function call<T>(path: string, token: string, { method, body }: { method?: string; body?: unknown } = {}): Promise<T> {
  let response: Response
  try {
    response = await fetch(`${accountUrl}${path}`, {
      method: method ?? (body === undefined ? 'GET' : 'POST'),
      headers: {
        Accept: 'application/json',
        Authorization: `Bearer ${token}`,
        ...(body !== undefined ? { 'Content-Type': 'application/json' } : {}),
      },
      body: body !== undefined ? JSON.stringify(body) : undefined,
    })
  } catch {
    throw new ApiError(0, 'Hesap bilgilerine ulaşılamıyor. Biraz sonra tekrar deneyin.')
  }

  if (response.status === 401) throw new ApiError(401, 'Oturumunuzun süresi doldu. Sayfayı yenileyin.')
  if (!response.ok) {
    const error: KeycloakError = await response.json().catch(() => ({}))
    const items = error.errors?.length ? error.errors : error.errorMessage ? [error] : []
    const fieldErrors: Record<string, string[]> = {}
    for (const item of items) (fieldErrors[item.field ?? ''] ??= []).push(fieldMessage(item))
    throw new ApiError(response.status, items.length ? 'Bilgiler kaydedilemedi.' : `İstek başarısız oldu (HTTP ${response.status}).`, fieldErrors)
  }

  return (response.status === 204 ? undefined : await response.json()) as T
}

export const accountApi = {
  profile: (token: string) => call<AccountProfile & Record<string, unknown>>('', token),

  // Keycloak gövdenin tamamını bekler: okunan bilgiler yalnızca ad ve soyad değiştirilerek geri gönderilir.
  // Alan tanımları (userProfileMetadata) yalnızca okuma için, geri gönderilmez.
  updateName: (current: AccountProfile & Record<string, unknown>, firstName: string, lastName: string, token: string) => {
    const { userProfileMetadata: _metadata, ...profile } = current
    return call<void>('', token, { body: { ...profile, firstName: firstName.trim(), lastName: lastName.trim() } })
  },

  // Açık oturumlar: bu cihaz önce, sonra son etkinliğe göre.
  sessions: async (token: string): Promise<AccountSession[]> => {
    const devices = await call<DeviceRepresentation[]>('/sessions/devices', token)
    return devices
      .flatMap((device) =>
        device.sessions.map((session) => ({
          id: session.id,
          ipAddress: session.ipAddress ?? '',
          started: session.started,
          lastAccess: session.lastAccess,
          expires: session.expires,
          // "Chrome/142.0.0" → "Chrome"; Keycloak tanıyamazsa "Other".
          browser: (session.browser ?? '').split('/')[0] || 'Bilinmeyen tarayıcı',
          os: [device.os, device.osVersion].filter((part) => part && part !== 'Other' && part !== 'Unknown').join(' ') || 'Bilinmeyen sistem',
          current: session.current === true,
        })),
      )
      .sort((a, b) => Number(b.current) - Number(a.current) || b.lastAccess - a.lastAccess)
  },

  // Keycloak oturumu kapatır; o cihazın elindeki token API'lerde süresi dolana kadar (en fazla 5 dk) geçerli kalır.
  signOutSession: (id: string, token: string) => call<void>(`/sessions/${encodeURIComponent(id)}`, token, { method: 'DELETE' }),

  // Bu cihaz dışındaki bütün oturumlar.
  signOutOtherSessions: (token: string) => call<void>('/sessions', token, { method: 'DELETE' }),

  // Kayıtlı doğrulayıcı uygulamalar; boşsa iki aşamalı doğrulama kapalı.
  otpDevices: async (token: string): Promise<OtpDevice[]> => {
    const types = await call<CredentialType[]>('/credentials?type=otp', token)
    return types
      .filter((type) => type.type === 'otp')
      .flatMap((type) => type.userCredentialMetadatas ?? [])
      .map(({ credential }) => ({ id: credential.id, label: credential.userLabel || 'Doğrulayıcı uygulama', createdDate: credential.createdDate }))
  },
}
