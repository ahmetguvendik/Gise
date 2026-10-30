import { useEffect } from 'react'
import { Link, NavLink, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from 'react-oidc-context'
import { accountUrl } from '../auth/oidcConfig'
import { useSessionReady } from '../auth/SilentSignin'
import { canCheckIn, canManageEvents, isAdmin } from '../auth/roles'
import { Logo, TicketMark } from './Logo'
import { Menu } from './Menu'

// Giriş ve kayıttan sonra kullanıcı bulunduğu sayfaya döner.
const here = () => window.location.origin + window.location.pathname

// "Yönetim" menüsündeki sayfalar; bunlardan birindeyken menü düğmesi seçili görünür.
const MANAGEMENT_PATHS = ['/kapi', '/etkinliklerim', '/onay-bekleyenler', '/mekan']

export function Layout() {
  const auth = useAuth()
  const sessionReady = useSessionReady()
  const { pathname } = useLocation()

  // Sayfa değişince en üstten başla; BrowserRouter kaydırma konumunu kendisi sıfırlamıyor.
  useEffect(() => {
    window.scrollTo(0, 0)
  }, [pathname])
  const navClass = ({ isActive }: { isActive: boolean }) => (isActive ? 'nav__link is-active' : 'nav__link')
  const itemClass = ({ isActive }: { isActive: boolean }) => (isActive ? 'menu__item is-active' : 'menu__item')

  // Rolüne göre yönetim sayfaları; hiçbiri yoksa menü görünmez.
  const gate = canCheckIn(auth.user)
  const organizer = canManageEvents(auth.user)
  const admin = isAdmin(auth.user)
  const name = auth.user?.profile.name ?? auth.user?.profile.preferred_username

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
            {(gate || organizer || admin) && (
              <Menu label="Yönetim" className="nav__link" active={MANAGEMENT_PATHS.some((path) => pathname.startsWith(path))}>
                {organizer && (
                  <NavLink to="/etkinliklerim" className={itemClass}>
                    Etkinliklerim
                  </NavLink>
                )}
                {admin && (
                  <>
                    <NavLink to="/onay-bekleyenler" className={itemClass}>
                      Onay bekleyenler
                    </NavLink>
                    <NavLink to="/mekanlar" className={itemClass}>
                      Mekânlar
                    </NavLink>
                  </>
                )}
                {gate && (
                  <NavLink to="/kapi" className={itemClass}>
                    Kapı
                  </NavLink>
                )}
              </Menu>
            )}
          </nav>

          {!sessionReady ? null : auth.isAuthenticated ? (
            <div className="header__user">
              <Menu label={<span className="header__name">{name}</span>} className="menu__button--account" align="end">
                <a className="menu__item" href={accountUrl}>
                  Hesabım
                </a>
                <button type="button" className="menu__item" onClick={() => void auth.signoutRedirect()}>
                  Çıkış
                </button>
              </Menu>
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
