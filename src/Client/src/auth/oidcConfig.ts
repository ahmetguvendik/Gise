import { InMemoryWebStorage, WebStorageStateStore } from 'oidc-client-ts'
import type { UserManagerSettings } from 'oidc-client-ts'
import type { AuthProviderProps } from 'react-oidc-context'

const authority = import.meta.env.VITE_KEYCLOAK_AUTHORITY ?? 'http://localhost:8180/realms/gise'

// Profil, parola, oturumlar ve hesap silme Keycloak'ın hesap sayfasında.
export const accountUrl = `${authority}/account`

// Giriş ve kayıt Keycloak'ın kendi sayfalarında yapılır (authorization code + PKCE); uygulama parolayı görmez.
export const userManagerSettings: UserManagerSettings = {
  authority,
  client_id: 'gise-web',
  redirect_uri: `${window.location.origin}/`,
  post_logout_redirect_uri: `${window.location.origin}/`,
  // Sessiz girişin gizli çerçevede döndüğü küçük sayfa (silent-renew.html).
  silent_redirect_uri: `${window.location.origin}/silent-renew.html`,
  // Keycloak'a ulaşılamazsa sayfa bundan uzun beklemesin; kullanıcı misafir olarak devam eder.
  silentRequestTimeoutInSeconds: 5,
  scope: 'openid profile email',
  // Token yalnızca bellekte: sayfadaki bir script localStorage'dan okuyamaz. Sayfa yenilenince bellek
  // sıfırlanır; açık Keycloak oturumu sessiz girişle geri alınır (bkz. SilentSignin).
  userStore: new WebStorageStateStore({ store: new InMemoryWebStorage() }),
}

export const oidcConfig: AuthProviderProps = {
  ...userManagerSettings,
  onSigninCallback: () => window.history.replaceState({}, document.title, window.location.pathname),
}
