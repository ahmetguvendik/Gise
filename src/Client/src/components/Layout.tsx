import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from 'react-oidc-context'
import { accountUrl } from '../auth/oidcConfig'
import { canManageEvents } from '../auth/roles'

// Giriş ve kayıttan sonra kullanıcı bulunduğu sayfaya döner.
const here = () => window.location.origin + window.location.pathname

export function Layout() {
  const auth = useAuth()
  const navClass = ({ isActive }: { isActive: boolean }) => (isActive ? 'nav__link is-active' : 'nav__link')

  return (
    <>
      <header className="header">
        <div className="container header__inner">
          <NavLink to="/" className="brand">
            Gişe
          </NavLink>

          <nav className="nav">
            <NavLink to="/" end className={navClass}>
              Etkinlikler
            </NavLink>
            {canManageEvents(auth.user) && (
              <NavLink to="/etkinliklerim" className={navClass}>
                Etkinliklerim
              </NavLink>
            )}
          </nav>

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
              <button
                className="button button--ghost"
                onClick={() => void auth.signinRedirect({ prompt: 'create', redirect_uri: here() })}
              >
                Kayıt ol
              </button>
              <button className="button" onClick={() => void auth.signinRedirect({ redirect_uri: here() })}>
                Giriş yap
              </button>
            </div>
          )}
        </div>
      </header>

      <main className="container page">
        <Outlet />
      </main>
    </>
  )
}
