import { accountUrl } from '../auth/oidcConfig'
import type { AccountProfile, OtpDevice } from '../types'
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

async function call<T>(path: string, token: string, body?: unknown): Promise<T> {
  let response: Response
  try {
    response = await fetch(`${accountUrl}${path}`, {
      method: body === undefined ? 'GET' : 'POST',
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
    return call<void>('', token, { ...profile, firstName: firstName.trim(), lastName: lastName.trim() })
  },

  // Kayıtlı doğrulayıcı uygulamalar; boşsa iki aşamalı doğrulama kapalı.
  otpDevices: async (token: string): Promise<OtpDevice[]> => {
    const types = await call<CredentialType[]>('/credentials?type=otp', token)
    return types
      .filter((type) => type.type === 'otp')
      .flatMap((type) => type.userCredentialMetadatas ?? [])
      .map(({ credential }) => ({ id: credential.id, label: credential.userLabel || 'Doğrulayıcı uygulama', createdDate: credential.createdDate }))
  },
}
