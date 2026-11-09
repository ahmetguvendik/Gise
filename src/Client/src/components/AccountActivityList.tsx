import { useEffect, useState } from 'react'
import { ApiError, toApiError } from '../api/http'
import { identityApi } from '../api/identity'
import { formatDate } from '../lib/format'
import type { AccountActivity } from '../types'
import { ErrorAlert } from './ErrorAlert'

// Keycloak olay türlerinin Türkçe karşılığı. Parola ve iki aşamalı doğrulama aynı türle gelir, credentialType ayırır.
function label({ type, error, credentialType }: AccountActivity) {
  const otp = credentialType === 'otp'
  switch (type) {
    case 'LOGIN':
      return 'Giriş yapıldı'
    case 'LOGIN_ERROR':
      if (error === 'invalid_user_credentials') return 'Hatalı parola ya da kodla giriş denemesi'
      if (error === 'user_temporarily_disabled' || error === 'user_disabled') return 'Kilitli hesaba giriş denemesi'
      return 'Başarısız giriş denemesi'
    case 'LOGOUT':
      return 'Çıkış yapıldı'
    case 'REGISTER':
      return 'Hesap oluşturuldu'
    case 'UPDATE_PROFILE':
      return 'Ad ya da soyad değiştirildi'
    case 'UPDATE_EMAIL':
      return 'E-posta adresi değiştirildi'
    case 'VERIFY_EMAIL':
      return 'E-posta adresi doğrulandı'
    case 'SEND_RESET_PASSWORD':
      return 'Parola sıfırlama e-postası istendi'
    case 'UPDATE_CREDENTIAL':
      return otp ? 'İki aşamalı doğrulama açıldı' : 'Parola değiştirildi'
    case 'UPDATE_CREDENTIAL_ERROR':
      return otp ? 'İki aşamalı doğrulama açılamadı' : 'Parola değiştirilemedi'
    case 'REMOVE_CREDENTIAL':
      return otp ? 'İki aşamalı doğrulama kaldırıldı' : 'Giriş yöntemi kaldırıldı'
    case 'REMOVE_CREDENTIAL_ERROR':
      return 'Giriş yöntemi kaldırılamadı'
    case 'USER_DISABLED_BY_TEMPORARY_LOCKOUT':
      return 'Çok fazla hatalı deneme: hesap geçici olarak kilitlendi'
    case 'USER_DISABLED_BY_PERMANENT_LOCKOUT':
      return 'Çok fazla hatalı deneme: hesap kilitlendi'
    default:
      return type
  }
}

// Profilde son girişler ve hesap güvenliği hareketleri (identity-api). Hatalı denemeler vurgulu.
export function AccountActivityList({ token }: { token: string }) {
  const [items, setItems] = useState<AccountActivity[] | null>(null)
  const [error, setError] = useState<ApiError | null>(null)

  useEffect(() => {
    let ignore = false
    identityApi.activity(token).then(
      (data) => {
        if (ignore) return
        setItems(data)
        setError(null)
      },
      (err) => {
        if (!ignore) setError(toApiError(err))
      },
    )
    return () => {
      ignore = true
    }
  }, [token])

  return (
    <section className="card profile__card" aria-labelledby="hareketler">
      <h2 id="hareketler">Son hareketler</h2>
      {error && <ErrorAlert error={error} />}

      {items === null && !error ? (
        <div className="skeleton skeleton--xs" />
      ) : items && items.length === 0 ? (
        <p className="muted">Henüz kayıtlı hareket yok.</p>
      ) : (
        items && (
          <ol className="activity">
            {items.map((item) => (
              <li key={`${item.time}-${item.type}`} className={item.failed ? 'activity__item activity__item--failed' : 'activity__item'}>
                <span className="activity__label">{label(item)}</span>
                <span className="activity__meta">
                  <time dateTime={item.time}>{formatDate(item.time)}</time>
                  {item.ipAddress && <span>IP {item.ipAddress}</span>}
                </span>
              </li>
            ))}
          </ol>
        )
      )}
      <p className="form__hint">
        Son 90 günün en fazla 20 hareketi. Sayfa yenilendiğinde yapılan sessiz giriş ayrıca sayılmaz; her oturumun ilk
        girişi görünür.
      </p>
    </section>
  )
}
