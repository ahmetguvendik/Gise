import { useEffect, useState } from 'react'
import { accountApi } from '../api/account'
import { ApiError, toApiError } from '../api/http'
import { formatDate } from '../lib/format'
import type { AccountSession } from '../types'
import { ErrorAlert } from './ErrorAlert'

const fromSeconds = (seconds: number) => formatDate(new Date(seconds * 1000).toISOString())

// Profilde açık oturumlar (Keycloak hesap API'si). Tanınmayan bir cihaz buradan kapatılır; kullanıcının kendi oturumu
// menüdeki "Çıkış"la kapanır.
export function AccountSessions({ token }: { token: string }) {
  const [sessions, setSessions] = useState<AccountSession[] | null>(null)
  const [error, setError] = useState<ApiError | null>(null)
  const [busy, setBusy] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [version, setVersion] = useState(0)

  useEffect(() => {
    let ignore = false
    accountApi.sessions(token).then(
      (data) => {
        if (ignore) return
        setSessions(data)
        setError(null)
      },
      (err) => {
        if (!ignore) setError(toApiError(err))
      },
    )
    return () => {
      ignore = true
    }
  }, [token, version])

  const run = async (key: string, action: () => Promise<void>, done: string) => {
    setBusy(key)
    setError(null)
    setNotice(null)
    try {
      await action()
      setNotice(done)
      setVersion((current) => current + 1)
    } catch (err) {
      setError(toApiError(err))
    } finally {
      setBusy(null)
    }
  }

  const others = sessions?.filter((session) => !session.current) ?? []

  return (
    <section className="card profile__card" aria-labelledby="oturumlar">
      <h2 id="oturumlar">Oturumlar</h2>
      {error && <ErrorAlert error={error} />}
      {notice && <div className="notice">{notice}</div>}

      {sessions === null && !error ? (
        <div className="skeleton skeleton--xs" />
      ) : (
        sessions && (
          <dl className="profile__list">
            {sessions.map((session) => (
              <div className="profile__item" key={session.id}>
                <div>
                  <dt>
                    {session.browser} · {session.os}
                    {session.current && <span className="badge badge--verified">Bu cihaz</span>}
                  </dt>
                  <dd className="profile__meta">
                    <span>IP {session.ipAddress || 'bilinmiyor'}</span>
                    <span>Giriş: {fromSeconds(session.started)}</span>
                    <span>Son etkinlik: {fromSeconds(session.lastAccess)}</span>
                  </dd>
                </div>
                {!session.current && (
                  <button
                    type="button"
                    className="button button--danger-outline"
                    disabled={busy !== null}
                    onClick={() => run(session.id, () => accountApi.signOutSession(session.id, token), 'Oturum kapatıldı.')}
                  >
                    {busy === session.id ? 'Kapatılıyor…' : 'Oturumu kapat'}
                  </button>
                )}
              </div>
            ))}
          </dl>
        )
      )}

      {others.length > 1 && (
        <button
          type="button"
          className="button button--danger-outline"
          disabled={busy !== null}
          onClick={() => {
            if (window.confirm(`Bu cihaz dışındaki ${others.length} oturum kapatılsın mı?`))
              void run('others', () => accountApi.signOutOtherSessions(token), 'Diğer oturumlar kapatıldı.')
          }}
        >
          {busy === 'others' ? 'Kapatılıyor…' : 'Diğer tüm oturumları kapat'}
        </button>
      )}
      <p className="form__hint">
        Tanımadığınız bir oturum görürseniz kapatın ve parolanızı değiştirin. Kapatılan cihaz en geç 5 dakika içinde
        oturumu kaybeder.
      </p>
    </section>
  )
}
