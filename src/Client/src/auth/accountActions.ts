import type { AuthContextProps } from 'react-oidc-context'

// Parola, e-posta, iki aşamalı doğrulama ve hesap silme Keycloak'ın kendi ekranlarında yapılır (application-initiated
// action): giriş isteğine kc_action eklenir, Keycloak ilgili ekranı açar (gerekirse parolayı yeniden sorar) ve
// kullanıcıyı /profil'e geri gönderir. Dönüş adresinde sonuç kc_action_status olarak gelir.
export type AccountAction = 'UPDATE_PASSWORD' | 'UPDATE_EMAIL' | 'CONFIGURE_TOTP' | 'delete_account' | `delete_credential:${string}`

export type AccountActionStatus = 'success' | 'cancelled' | 'error'

export interface AccountActionResult {
  action: string
  status: AccountActionStatus
}

const PENDING_KEY = 'gise.accountAction'
let lastResult: AccountActionResult | null = null

export function startAccountAction(auth: AuthContextProps, action: AccountAction) {
  // Keycloak dönüşte işlemin adını her zaman eklemiyor; hangi işlem olduğu yönlendirmeden önce saklanır.
  try {
    sessionStorage.setItem(PENDING_KEY, action)
  } catch {
    // Depolama kapalıysa sonuç mesajı genel olur.
  }
  return auth.signinRedirect({ redirect_uri: `${window.location.origin}/profil`, extraQueryParams: { kc_action: action } })
}

// Açılışta, React çizmeden ve giriş dönüşü adres çubuğunu temizlemeden önce çağrılır (main.tsx).
export function captureAccountActionResult() {
  const params = new URLSearchParams(window.location.search)
  const status = params.get('kc_action_status')
  if (status !== 'success' && status !== 'cancelled' && status !== 'error') return

  let pending: string | null = null
  try {
    pending = sessionStorage.getItem(PENDING_KEY)
    sessionStorage.removeItem(PENDING_KEY)
  } catch {
    // Depolama kapalı.
  }
  lastResult = { action: params.get('kc_action') ?? pending ?? '', status }
}

/** Keycloak'tan dönülen işlemin sonucu; sayfa gösterince clearAccountActionResult ile silinir. */
export const accountActionResult = () => lastResult

// Okuma ve silme ayrı: geliştirmede StrictMode bileşeni iki kez çizer, okuyunca silinse ikinci çizimde mesaj kaybolurdu.
export function clearAccountActionResult() {
  lastResult = null
}
