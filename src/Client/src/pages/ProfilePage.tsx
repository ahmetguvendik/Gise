import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { useAuth } from 'react-oidc-context'
import { accountApi } from '../api/account'
import { ApiError, toApiError } from '../api/http'
import { accountActionResult, clearAccountActionResult, startAccountAction } from '../auth/accountActions'
import type { AccountAction, AccountActionResult } from '../auth/accountActions'
import { ROLE_LABELS, rolesOf } from '../auth/roles'
import { ErrorAlert } from '../components/ErrorAlert'
import { formatLongDate } from '../lib/format'
import type { AccountProfile, OtpDevice } from '../types'

type Profile = AccountProfile & Record<string, unknown>

const SUCCESS_MESSAGES: Record<string, string> = {
  UPDATE_PASSWORD: 'Parolanız değiştirildi.',
  UPDATE_EMAIL: 'Yeni adresinize bir doğrulama bağlantısı gönderildi. Bağlantıya tıklayınca e-postanız değişir.',
  CONFIGURE_TOTP: 'İki aşamalı doğrulama açıldı. Bundan sonra girişte doğrulayıcı uygulamadaki kod da sorulur.',
  delete_credential: 'Doğrulayıcı uygulama kaldırıldı.',
}

function resultNotice({ action, status }: AccountActionResult) {
  if (status === 'cancelled') return { ok: false, text: 'İşlem iptal edildi; hesabınızda bir şey değişmedi.' }
  if (status === 'error') return { ok: false, text: 'İşlem tamamlanamadı. Biraz sonra tekrar deneyin.' }
  return { ok: true, text: SUCCESS_MESSAGES[action.split(':')[0]] ?? 'İşlem tamamlandı.' }
}

// Guard'ın arkasında. Ad ve soyad burada değişir (Keycloak hesap API'si); parola, e-posta, iki aşamalı doğrulama ve
// hesap silme Keycloak'ın kendi ekranında yapılıp buraya dönülür (bkz. accountActions).
export function ProfilePage() {
  const auth = useAuth()
  const token = auth.user?.access_token ?? ''
  const [profile, setProfile] = useState<Profile | null>(null)
  const [otpDevices, setOtpDevices] = useState<OtpDevice[]>([])
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [loadError, setLoadError] = useState<ApiError | null>(null)
  const [saveError, setSaveError] = useState<ApiError | null>(null)
  const [busy, setBusy] = useState(false)
  const [saved, setSaved] = useState(false)
  const [redirecting, setRedirecting] = useState(false)
  // Keycloak'tan dönülüyorsa işlemin sonucu; sayfa açılışında bir kez alınır.
  const [actionResult] = useState(accountActionResult)
  // Bilgiler yalnızca açılışta çekilir: token sessiz yenilemeyle değişince yeniden çekilirse formdaki değişiklik kaybolur.
  const [initialToken] = useState(token)

  // Sonuç bir kez gösterilir; sayfaya sonradan yeniden gelince görünmez.
  useEffect(clearAccountActionResult, [])

  useEffect(() => {
    let ignore = false
    Promise.all([accountApi.profile(initialToken), accountApi.otpDevices(initialToken)]).then(
      ([profileData, devices]) => {
        if (ignore) return
        setProfile(profileData)
        setFirstName(profileData.firstName ?? '')
        setLastName(profileData.lastName ?? '')
        setOtpDevices(devices)
        setLoadError(null)
      },
      (err) => {
        if (!ignore) setLoadError(toApiError(err))
      },
    )
    return () => {
      ignore = true
    }
  }, [initialToken])

  const dirty = profile !== null && (firstName.trim() !== (profile.firstName ?? '') || lastName.trim() !== (profile.lastName ?? ''))

  const change = (apply: () => void) => {
    apply()
    setSaved(false)
  }

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    if (!profile) return
    setBusy(true)
    setSaveError(null)
    try {
      await accountApi.updateName(profile, firstName, lastName, token)
      setProfile({ ...profile, firstName: firstName.trim(), lastName: lastName.trim() })
      setSaved(true)
      // Üst menüdeki ad token'dan okunur; yeni token yeni adı taşır. Yenilenemezse bir sonraki girişte güncellenir.
      await auth.signinSilent().catch(() => undefined)
    } catch (err) {
      setSaveError(toApiError(err))
    } finally {
      setBusy(false)
    }
  }

  const go = (action: AccountAction) => {
    setRedirecting(true)
    startAccountAction(auth, action).catch(() => setRedirecting(false))
  }

  const notice = actionResult && resultNotice(actionResult)
  const roles = rolesOf(auth.user)
    .map((role) => ROLE_LABELS[role])
    .filter(Boolean)

  return (
    <div className="container page">
      <h1 className="page-title">Profilim</h1>

      <div className="profile">
        {notice && <div className={notice.ok ? 'notice' : 'alert'}>{notice.text}</div>}
        {loadError && <ErrorAlert error={loadError} />}

        {profile === null && !loadError ? (
          <div className="skeleton skeleton--sm" />
        ) : (
          profile && (
            <>
              <section className="card profile__card" aria-labelledby="kisisel">
                <h2 id="kisisel">Kişisel bilgiler</h2>
                <form className="form" onSubmit={submit}>
                  {saveError && <ErrorAlert error={saveError} />}
                  {saved && <div className="notice">Bilgileriniz kaydedildi.</div>}
                  <div className="form__row">
                    <label>
                      Ad
                      <input
                        value={firstName}
                        onChange={(e) => change(() => setFirstName(e.target.value))}
                        maxLength={255}
                        autoComplete="given-name"
                        required
                      />
                    </label>
                    <label>
                      Soyad
                      <input
                        value={lastName}
                        onChange={(e) => change(() => setLastName(e.target.value))}
                        maxLength={255}
                        autoComplete="family-name"
                        required
                      />
                    </label>
                    <button className="button button--contrast" disabled={busy || !dirty}>
                      {busy ? 'Kaydediliyor…' : 'Kaydet'}
                    </button>
                  </div>
                </form>
              </section>

              <section className="card profile__card" aria-labelledby="hesap">
                <h2 id="hesap">Hesap</h2>
                <dl className="profile__list">
                  <div className="profile__item">
                    <div>
                      <dt>E-posta</dt>
                      <dd>
                        <span className="profile__value">{profile.email}</span>
                        {profile.emailVerified ? (
                          <span className="badge badge--verified">Doğrulandı</span>
                        ) : (
                          <span className="badge badge--unverified">Doğrulanmadı</span>
                        )}
                      </dd>
                    </div>
                    <button type="button" className="button button--outline" disabled={redirecting} onClick={() => go('UPDATE_EMAIL')}>
                      E-postayı değiştir
                    </button>
                  </div>
                  <div className="profile__item">
                    <div>
                      <dt>Roller</dt>
                      <dd>{roles.length > 0 ? roles.join(', ') : 'Yok'}</dd>
                    </div>
                  </div>
                </dl>
                <p className="form__hint">
                  Yeni adrese bir doğrulama bağlantısı gönderilir; bağlantıya tıklanana kadar e-postanız değişmez. Gelen
                  bilet aktarımları doğrulanmış adresinize göre gösterilir.
                </p>
              </section>

              <section className="card profile__card" aria-labelledby="guvenlik">
                <h2 id="guvenlik">Güvenlik</h2>
                <dl className="profile__list">
                  <div className="profile__item">
                    <div>
                      <dt>Parola</dt>
                      <dd>Değiştirirken mevcut parolanız sorulabilir.</dd>
                    </div>
                    <button type="button" className="button button--outline" disabled={redirecting} onClick={() => go('UPDATE_PASSWORD')}>
                      Parolayı değiştir
                    </button>
                  </div>
                  <div className="profile__item">
                    <div>
                      <dt>İki aşamalı doğrulama</dt>
                      <dd>
                        {otpDevices.length > 0 ? (
                          <span className="badge badge--verified">Açık</span>
                        ) : (
                          <span className="badge badge--unverified">Kapalı</span>
                        )}
                        <span>Girişte parolaya ek olarak doğrulayıcı uygulamadaki kod sorulur.</span>
                      </dd>
                    </div>
                    {otpDevices.length === 0 && (
                      <button type="button" className="button button--outline" disabled={redirecting} onClick={() => go('CONFIGURE_TOTP')}>
                        Aç
                      </button>
                    )}
                  </div>
                  {otpDevices.map((device) => (
                    <div className="profile__item profile__item--nested" key={device.id}>
                      <div>
                        <dt>{device.label}</dt>
                        {device.createdDate && <dd>Eklenme: {formatLongDate(new Date(device.createdDate).toISOString())}</dd>}
                      </div>
                      <button
                        type="button"
                        className="button button--danger-outline"
                        disabled={redirecting}
                        onClick={() => go(`delete_credential:${device.id}`)}
                      >
                        Kaldır
                      </button>
                    </div>
                  ))}
                </dl>
              </section>

              <section className="card profile__card profile__danger" aria-labelledby="hesap-sil">
                <h2 id="hesap-sil">Hesabı sil</h2>
                <p className="muted">
                  Hesabınız kalıcı olarak silinir ve biletlerinize, siparişlerinize bir daha ulaşamazsınız; bu geri
                  alınamaz. Bir sonraki ekranda onayınız istenir.
                </p>
                <button type="button" className="button button--danger-outline" disabled={redirecting} onClick={() => go('delete_account')}>
                  Hesabımı sil
                </button>
              </section>
            </>
          )
        )}
      </div>
    </div>
  )
}
