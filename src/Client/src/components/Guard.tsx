import type { ReactNode } from 'react'
import { useAuth } from 'react-oidc-context'
import { ROLE_LABELS, hasAnyRole, rolesOf } from '../auth/roles'

/**
 * Route seviyesinde erişim kontrolü. Bu yalnızca arayüzü gizler —
 * asıl yetkilendirme backend'deki [Authorize(Policy = ...)] ile yapılır.
 *
 * `roles` verilmezse yalnızca giriş yapmış olmak yeterlidir.
 */
export function Guard({ roles, children }: { roles?: string[]; children: ReactNode }) {
  const auth = useAuth()

  if (auth.isLoading) return <p className="muted">Yükleniyor…</p>

  if (!auth.isAuthenticated) {
    return (
      <div className="card empty">
        <h1>Bu sayfa için giriş yapmalısınız</h1>
        <button
          className="button"
          onClick={() => void auth.signinRedirect({ redirect_uri: window.location.origin + window.location.pathname })}
        >
          Giriş yap
        </button>
      </div>
    )
  }

  if (roles && !hasAnyRole(auth.user, roles)) {
    return (
      <div className="card empty">
        <h1>Bu sayfa için yetkiniz yok</h1>
        <p className="muted">
          Gerekli rol: <strong>{roles.map((role) => ROLE_LABELS[role] ?? role).join(' veya ')}</strong>. Mevcut
          rolleriniz: <strong>{rolesOf(auth.user).map((role) => ROLE_LABELS[role]).filter(Boolean).join(', ') || 'yok'}</strong>.
        </p>
      </div>
    )
  }

  return <>{children}</>
}
