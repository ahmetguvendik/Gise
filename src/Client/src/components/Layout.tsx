import { useEffect } from 'react'
import { Link, NavLink, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from 'react-oidc-context'
import { accountUrl } from '../auth/oidcConfig'
import { useSessionReady } from '../auth/SilentSignin'
import { canCheckIn, canManageEvents, isAdmin } from '../auth/roles'
import { Logo, TicketMark } from './Logo'

// Giriş ve kayıttan sonra kullanıcı bulunduğu sayfaya döner.
const here = () => window.location.origin + window.location.pathname

export function Layout() {
  const auth = useAuth()
  const sessionReady = useSessionReady()
  const { pathname } = useLocation()

  // Sayfa değişince en üstten başla; BrowserRouter kaydırma konumunu kendisi sıfırlamıyor.
  useEffect(() => {
    window.scrollTo(0, 0)
  }, [pathname])
  const navClass = ({ isActive }: { isActive: boolean }) => (isActive ? 'nav__link is-active' : 'nav__link')

  return (
    <>
      <header className="header">
        <div className="container header__inner">
          <Link to="/" className="brand" aria-label="Gişe ana sayfa">
            <TicketMark />
            <span className="brand__name">Gişe</span>
          </Link>

          <nav className="nav" aria-label="Ana menü">
            <NavLink to="/" end className={navClass}>
              Etkinlikler
            </NavLink>
            {sessionReady && auth.isAuthenticated && (
              <>
                <NavLink to="/biletlerim" className={navClass}>
                  Biletlerim
                </NavLink>
                <NavLink to="/siparislerim" className={navClass}>
                  Siparişlerim
                </NavLink>
              </>
            )}
            {canCheckIn(auth.user) && (
              <NavLink to="/kapi" className={navClass}>
                Kapı
              </NavLink>
            )}
            {canManageEvents(auth.user) && (
              <NavLink to="/etkinliklerim" className={navClass}>
                Etkinliklerim
              </NavLink>
            )}
            {isAdmin(auth.user) && (
              <>
                <NavLink to="/onay-bekleyenler" className={navClass}>
                  Onay bekleyenler
                </NavLink>
                <NavLink to="/mekanlar" className={navClass}>
                  Mekânlar
                </NavLink>
              </>
            )}
          </nav>

          {!sessionReady ? null : auth.isAuthenticated ? (
            <div className="header__user">
              <span className="header__name">{auth.user?.profile.name ?? auth.user?.profile.preferred_username}</span>
              <a className="button button--outline" href={accountUrl}>
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
                className="button button--outline"
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

      <main>
        <Outlet />
      </main>

      <footer className="footer">
        <div className="container footer__inner">
          <Logo small />
          <p className="muted">Konser, tiyatro ve gösteri biletleri</p>
        </div>
      </footer>
    </>
  )
}
