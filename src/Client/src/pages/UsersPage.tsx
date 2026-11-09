import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { useAuth } from 'react-oidc-context'
import { ApiError, toApiError } from '../api/http'
import { usersApi } from '../api/users'
import { ROLE_LABELS } from '../auth/roles'
import { ErrorAlert } from '../components/ErrorAlert'
import { Pagination } from '../components/Pagination'
import type { ManagedUser, PagedResult } from '../types'

// Uygulamadan verilip alınabilen roller; "customer" herkesin varsayılan rolü, burada yok.
const MANAGED_ROLES = ['organizer', 'gate-staff', 'admin']

// Guard'ın arkasında: yalnızca admin. Açılışta bütün kullanıcılar, sayfada 10 (yalnızca o sayfa çekilir); arama e-posta ya
// da adla daraltır ve ilk sayfaya döner. Rol verme/alma identity-api üzerinden Keycloak'ta.
export function UsersPage() {
  const auth = useAuth()
  const token = auth.user?.access_token ?? ''
  const currentUserId = auth.user?.profile.sub
  const [search, setSearch] = useState('')
  // Listenin şu an gösterdiği arama (kutudaki yazı değişse de sayfa değişimi buna göre).
  const [shownSearch, setShownSearch] = useState('')
  const [result, setResult] = useState<PagedResult<ManagedUser> | null>(null)
  const [loading, setLoading] = useState(false)
  const [busy, setBusy] = useState<string | null>(null)
  const [error, setError] = useState<ApiError | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  // Açılıştaki token'la ilk sayfa; sessiz yenilemede liste yeniden yüklenmez.
  const [initialToken] = useState(token)
  const users = result?.items ?? null

  useEffect(() => {
    let ignore = false
    usersApi.list('', 1, initialToken).then(
      (data) => {
        if (!ignore) setResult(data)
      },
      (err) => {
        if (!ignore) setError(toApiError(err))
      },
    )
    return () => {
      ignore = true
    }
  }, [initialToken])

  const load = async (term: string, page: number) => {
    setLoading(true)
    setError(null)
    setNotice(null)
    try {
      setResult(await usersApi.list(term, page, token))
      setShownSearch(term)
      window.scrollTo(0, 0)
    } catch (err) {
      setError(toApiError(err))
    } finally {
      setLoading(false)
    }
  }

  const submit = (e: FormEvent) => {
    e.preventDefault()
    void load(search.trim(), 1)
  }

  const toggle = async (user: ManagedUser, role: string) => {
    const has = user.roles.includes(role)
    const label = ROLE_LABELS[role] ?? role
    if (role === 'admin' && !window.confirm(has ? `${user.email} artık yönetici olmayacak. Devam edilsin mi?` : `${user.email} yönetici olacak: her şeyi yönetebilir, başkasına rol verebilir. Devam edilsin mi?`))
      return

    setBusy(`${user.id}:${role}`)
    setError(null)
    setNotice(null)
    try {
      if (has) await usersApi.revokeRole(user.id, role, token)
      else await usersApi.grantRole(user.id, role, token)
      setResult((current) =>
        current && {
          ...current,
          items: current.items.map((u) =>
            u.id === user.id ? { ...u, roles: has ? u.roles.filter((r) => r !== role) : [...u.roles, role] } : u,
          ),
        },
      )
      setNotice(`${user.email}: ${label} rolü ${has ? 'alındı' : 'verildi'}. Kullanıcının oturumu yenilenince (en geç 5 dakika) geçerli olur.`)
    } catch (err) {
      setError(toApiError(err))
    } finally {
      setBusy(null)
    }
  }

  return (
    <div className="container page">
      <div className="stack">
        <p className="eyebrow">Yönetici</p>
        <h1 className="page-title">Kullanıcılar</h1>
        <p className="muted">Organizatör, kapı görevlisi ya da yönetici rolü verin veya alın. Aramayla e-posta ya da ada göre daraltın.</p>
      </div>

      <form className="form users-search" onSubmit={submit}>
        <div className="form__row">
          <label>
            E-posta ya da ad
            <input
              type="search"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              maxLength={100}
              placeholder="ör. ayse@ornek.com"
            />
          </label>
          {/* Boş arama bütün kullanıcıları yeniden listeler; tek harf aranmaz. */}
          <button className="button button--contrast" disabled={loading || search.trim().length === 1}>
            {loading ? 'Yükleniyor…' : search.trim() ? 'Ara' : 'Tümünü göster'}
          </button>
        </div>
      </form>

      {error && <ErrorAlert error={error} />}
      {notice && <div className="notice">{notice}</div>}

      {users === null && !error && <div className="skeleton skeleton--sm" />}
      {users && users.length === 0 && (
        <p className="muted">{shownSearch ? 'Bu aramayla eşleşen kullanıcı yok.' : 'Henüz kullanıcı yok.'}</p>
      )}

      {users && users.length > 0 && (
        <ul className="user-list">
          {users.map((user) => {
            const name = [user.firstName, user.lastName].filter(Boolean).join(' ')
            const self = user.id === currentUserId
            return (
              <li key={user.id} className="card user-row">
                <div className="user-row__who">
                  <strong>{name || user.email}</strong>
                  <span className="muted">
                    {user.email}
                    {self && ' · siz'}
                  </span>
                  <span className="user-row__badges">
                    {!user.emailVerified && <span className="badge badge--unverified">E-posta doğrulanmadı</span>}
                    {!user.enabled && <span className="badge badge--cancelled">Devre dışı</span>}
                  </span>
                  {!user.emailVerified && (
                    <span className="user-row__hint">Rol vermek için kullanıcının önce e-posta adresini doğrulaması gerekiyor.</span>
                  )}
                </div>
                <div className="user-row__roles" role="group" aria-label={`${user.email} rolleri`}>
                  {MANAGED_ROLES.map((role) => {
                    const has = user.roles.includes(role)
                    // Kendi yönetici rolünü kaldırmak yok: sistem adminsiz kalmasın. E-postasını doğrulamamış kullanıcıya rol
                    // verilmez; önceden verilmiş rol alınabilir. İkisini API de reddeder.
                    const ownAdmin = self && role === 'admin'
                    const unverified = !has && !user.emailVerified
                    const locked = ownAdmin || unverified
                    return (
                      <button
                        key={role}
                        type="button"
                        className={has ? 'role-toggle is-on' : 'role-toggle'}
                        aria-pressed={has}
                        disabled={busy !== null || locked}
                        title={
                          ownAdmin
                            ? 'Kendi yönetici rolünüzü kaldıramazsınız.'
                            : unverified
                              ? 'E-postasını doğrulamamış kullanıcıya rol verilemez.'
                              : undefined
                        }
                        onClick={() => void toggle(user, role)}
                      >
                        {busy === `${user.id}:${role}` ? '…' : ROLE_LABELS[role]}
                      </button>
                    )
                  })}
                </div>
              </li>
            )
          })}
        </ul>
      )}
      {result && (
        <Pagination
          page={result.page}
          pageSize={result.pageSize}
          totalCount={result.totalCount}
          disabled={loading || busy !== null}
          onPageChange={(page) => void load(shownSearch, page)}
        />
      )}
    </div>
  )
}
