import { useState } from 'react'
import type { FormEvent } from 'react'
import { useAuth } from 'react-oidc-context'
import { ApiError, toApiError } from '../api/http'
import { usersApi } from '../api/users'
import { ROLE_LABELS } from '../auth/roles'
import { ErrorAlert } from '../components/ErrorAlert'
import type { ManagedUser } from '../types'

// Uygulamadan verilip alınabilen roller; "customer" herkesin varsayılan rolü, burada yok.
const MANAGED_ROLES = ['organizer', 'gate-staff', 'admin']

// Guard'ın arkasında: yalnızca admin. Kullanıcıyı e-postası ya da adıyla bulup rol verir/alır (identity-api, Keycloak).
export function UsersPage() {
  const auth = useAuth()
  const token = auth.user?.access_token ?? ''
  const currentUserId = auth.user?.profile.sub
  const [search, setSearch] = useState('')
  const [users, setUsers] = useState<ManagedUser[] | null>(null)
  const [searching, setSearching] = useState(false)
  const [busy, setBusy] = useState<string | null>(null)
  const [error, setError] = useState<ApiError | null>(null)
  const [notice, setNotice] = useState<string | null>(null)

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    setSearching(true)
    setError(null)
    setNotice(null)
    try {
      setUsers(await usersApi.search(search.trim(), token))
    } catch (err) {
      setError(toApiError(err))
    } finally {
      setSearching(false)
    }
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
      setUsers((current) =>
        current?.map((u) =>
          u.id === user.id ? { ...u, roles: has ? u.roles.filter((r) => r !== role) : [...u.roles, role] } : u,
        ) ?? null,
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
        <p className="muted">Kullanıcıyı e-postası ya da adıyla bulun; organizatör, kapı görevlisi ya da yönetici rolü verin veya alın.</p>
      </div>

      <form className="form users-search" onSubmit={submit}>
        <div className="form__row">
          <label>
            E-posta ya da ad
            <input
              type="search"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              minLength={2}
              maxLength={100}
              placeholder="ör. ayse@ornek.com"
              required
            />
          </label>
          <button className="button button--contrast" disabled={searching || search.trim().length < 2}>
            {searching ? 'Aranıyor…' : 'Ara'}
          </button>
        </div>
      </form>

      {error && <ErrorAlert error={error} />}
      {notice && <div className="notice">{notice}</div>}

      {users && users.length === 0 && <p className="muted">Bu aramayla eşleşen kullanıcı yok.</p>}

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
                </div>
                <div className="user-row__roles" role="group" aria-label={`${user.email} rolleri`}>
                  {MANAGED_ROLES.map((role) => {
                    const has = user.roles.includes(role)
                    // Kendi yönetici rolünü kaldırmak yok: sistem adminsiz kalmasın (API de reddeder).
                    const locked = self && role === 'admin'
                    return (
                      <button
                        key={role}
                        type="button"
                        className={has ? 'role-toggle is-on' : 'role-toggle'}
                        aria-pressed={has}
                        disabled={busy !== null || locked}
                        title={locked ? 'Kendi yönetici rolünüzü kaldıramazsınız.' : undefined}
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
      {users && users.length === 20 && <p className="muted">İlk 20 sonuç gösteriliyor; aramayı daraltın.</p>}
    </div>
  )
}
