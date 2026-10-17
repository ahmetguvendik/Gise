import { InMemoryWebStorage, WebStorageStateStore } from 'oidc-client-ts'
import type { AuthProviderProps } from 'react-oidc-context'

const authority = import.meta.env.VITE_KEYCLOAK_AUTHORITY ?? 'http://localhost:8180/realms/gise'

// Profil, parola, oturumlar ve hesap silme Keycloak'ın hesap sayfasında.
export const accountUrl = `${authority}/account`

// Giriş ve kayıt Keycloak'ın kendi sayfalarında yapılır (authorization code + PKCE); uygulama parolayı görmez.
export const oidcConfig: AuthProviderProps = {
  authority,
  client_id: 'gise-web',
  redirect_uri: `${window.location.origin}/`,
  post_logout_redirect_uri: `${window.location.origin}/`,
  scope: 'openid profile email',
  // Token yalnızca bellekte: sayfadaki bir script localStorage'dan okuyamaz. Bedeli, sayfa
  // yenilenince yeniden "Giriş yap" gerekmesi (Keycloak oturumu açık olduğu için parola sorulmaz).
  userStore: new WebStorageStateStore({ store: new InMemoryWebStorage() }),
  onSigninCallback: () => window.history.replaceState({}, document.title, window.location.pathname),
}
