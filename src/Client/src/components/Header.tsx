import { useAuth } from 'react-oidc-context'
import { accountUrl } from '../auth/oidcConfig'

export function Header() {
  const auth = useAuth()

  return (
    <header className="header">
      <div className="container header__inner">
        <span className="brand">Gişe</span>

        {auth.isLoading ? null : auth.isAuthenticated ? (
          <div className="header__user">
            <span>{auth.user?.profile.name ?? auth.user?.profile.preferred_username}</span>
            <a className="button button--ghost" href={accountUrl}>
              Hesabım
            </a>
            <button className="button button--ghost" onClick={() => void auth.signoutRedirect()}>
              Çıkış
            </button>
          </div>
        ) : (
          <div className="header__user">
            {/* prompt=create: Keycloak giriş yerine doğrudan kayıt formunu açar. */}
            <button className="button button--ghost" onClick={() => void auth.signinRedirect({ prompt: 'create' })}>
              Kayıt ol
            </button>
            <button className="button" onClick={() => void auth.signinRedirect()}>
              Giriş yap
            </button>
          </div>
        )}
      </div>
    </header>
  )
}
