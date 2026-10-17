import { useAuth } from 'react-oidc-context'

export function Header() {
  const auth = useAuth()

  return (
    <header className="header">
      <div className="container header__inner">
        <span className="brand">Gişe</span>

        {auth.isLoading ? null : auth.isAuthenticated ? (
          <div className="header__user">
            <span>{auth.user?.profile.name ?? auth.user?.profile.preferred_username}</span>
            <button className="button button--ghost" onClick={() => void auth.signoutRedirect()}>
              Çıkış
            </button>
          </div>
        ) : (
          <button className="button" onClick={() => void auth.signinRedirect()}>
            Giriş yap
          </button>
        )}
      </div>
    </header>
  )
}
