import { UserManager } from 'oidc-client-ts'
import { userManagerSettings } from './auth/oidcConfig'

// Gizli çerçevede açılır: Keycloak'tan dönen sonucu (kod ya da "oturum yok") ana sayfaya iletir.
void new UserManager(userManagerSettings).signinSilentCallback()
