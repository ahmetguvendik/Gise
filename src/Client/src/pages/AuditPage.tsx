import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { useAuth } from 'react-oidc-context'
import { ApiError, toApiError } from '../api/http'
import { auditApi } from '../api/audit'
import type { AuditFilter } from '../api/audit'
import { ErrorAlert } from '../components/ErrorAlert'
import { Pagination } from '../components/Pagination'
import type { AuditRecord, PagedResult } from '../types'

// Bilinen olay türleri ve Türkçe adları; listede olmayan tür ham adıyla gösterilir.
const ACTIONS: Record<string, string> = {
  Login: 'Giriş',
  Logout: 'Çıkış',
  Register: 'Kayıt',
  VerifyEmail: 'E-posta doğrulama',
  UpdateCredential: 'Parola/doğrulama değişikliği',
  GrantRole: 'Rol verme',
  RevokeRole: 'Rol alma',
  CreateVenue: 'Mekân oluşturma',
  CreateEvent: 'Etkinlik oluşturma',
  SubmitEvent: 'Etkinliği onaya gönderme',
  PublishEvent: 'Etkinliği yayınlama',
  SetEventPrices: 'Fiyat belirleme',
  SetEventPoster: 'Afiş belirleme',
  CreateUploadUrl: 'Yükleme adresi alma',
  CommitUpload: 'Yüklemeyi tamamlama',
  CreateOrder: 'Sipariş oluşturma',
  PayOrder: 'Ödeme',
  OfferTicketTransfer: 'Bilet aktarma teklifi',
  AcceptTicketTransfer: 'Bilet aktarımını kabul',
}

const OUTCOMES: Record<string, string> = { success: 'Başarılı', failure: 'Başarısız', unknown: 'Bilinmiyor' }

const actionLabel = (action: string) => ACTIONS[action] ?? action
const dayStart = (value: string) => (value ? new Date(`${value}T00:00:00`).toISOString() : undefined)
const dayEnd = (value: string) => (value ? new Date(`${value}T23:59:59.999`).toISOString() : undefined)

// Guard'ın arkasında: yalnızca admin. Kayıtlar audit-api'den (Elasticsearch, 365 gün); yeniden eskiye, sayfada 20.
// Süzgeçler birlikte uygulanır; bir kullanıcının kimliğine tıklamak o kullanıcının kayıtlarına daraltır.
export function AuditPage() {
  const auth = useAuth()
  const token = auth.user?.access_token ?? ''
  const [action, setAction] = useState('')
  const [outcome, setOutcome] = useState('')
  const [actorId, setActorId] = useState('')
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  // Listenin şu an gösterdiği süzgeç (formdaki değerler değişse de sayfa değişimi buna göre).
  const [shown, setShown] = useState<AuditFilter>({})
  const [result, setResult] = useState<PagedResult<AuditRecord> | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<ApiError | null>(null)
  // Açılıştaki token'la ilk sayfa; sessiz yenilemede liste yeniden yüklenmez.
  const [initialToken] = useState(token)
  const records = result?.items ?? null

  useEffect(() => {
    let ignore = false
    auditApi.list({}, 1, initialToken).then(
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

  const load = async (filter: AuditFilter, page: number) => {
    setLoading(true)
    setError(null)
    try {
      setResult(await auditApi.list(filter, page, token))
      setShown(filter)
      window.scrollTo(0, 0)
    } catch (err) {
      setError(toApiError(err))
    } finally {
      setLoading(false)
    }
  }

  const submit = (e: FormEvent) => {
    e.preventDefault()
    void load({ action, outcome, actorId: actorId.trim(), from: dayStart(from), to: dayEnd(to) }, 1)
  }

  const filterByActor = (id: string) => {
    setActorId(id)
    void load({ action, outcome, actorId: id, from: dayStart(from), to: dayEnd(to) }, 1)
  }

  const reset = () => {
    setAction('')
    setOutcome('')
    setActorId('')
    setFrom('')
    setTo('')
    void load({}, 1)
  }

  return (
    <div className="container page">
      <div className="stack">
        <p className="eyebrow">Yönetici</p>
        <h1 className="page-title">Denetim kayıtları</h1>
        <p className="muted">Kim, ne zaman, neyi yaptı: girişler, rol değişiklikleri ve iş işlemleri. Kayıtlar 1 yıl saklanır.</p>
      </div>

      <form className="form audit-filter" onSubmit={submit}>
        <div className="form__row">
          <label>
            İşlem
            <select value={action} onChange={(e) => setAction(e.target.value)}>
              <option value="">Hepsi</option>
              {Object.entries(ACTIONS).map(([value, label]) => (
                <option key={value} value={value}>
                  {label}
                </option>
              ))}
            </select>
          </label>
          <label>
            Sonuç
            <select value={outcome} onChange={(e) => setOutcome(e.target.value)}>
              <option value="">Hepsi</option>
              {Object.entries(OUTCOMES).map(([value, label]) => (
                <option key={value} value={value}>
                  {label}
                </option>
              ))}
            </select>
          </label>
          <label>
            Başlangıç
            <input type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
          </label>
          <label>
            Bitiş
            <input type="date" value={to} min={from || undefined} onChange={(e) => setTo(e.target.value)} />
          </label>
        </div>
        <div className="form__row">
          <label>
            Kullanıcı kimliği
            <input value={actorId} onChange={(e) => setActorId(e.target.value)} maxLength={100} placeholder="ör. 385e1bd8-1e2e-42d1-bf15-fc262c5332b9" />
          </label>
          <button className="button button--contrast" disabled={loading}>
            {loading ? 'Yükleniyor…' : 'Süz'}
          </button>
          <button type="button" className="button button--outline" disabled={loading} onClick={reset}>
            Temizle
          </button>
        </div>
      </form>

      {error && <ErrorAlert error={error} />}

      {records === null && !error && <div className="skeleton skeleton--sm" />}
      {records && records.length === 0 && (
        <p className="muted">{Object.values(shown).some(Boolean) ? 'Bu süzgeçle eşleşen kayıt yok.' : 'Henüz kayıt yok.'}</p>
      )}

      {records && records.length > 0 && (
        <ul className="audit-list">
          {records.map((record) => (
            <li key={record.id} className="card audit-row">
              <div className="audit-row__head">
                <strong>{actionLabel(record.action)}</strong>
                <span className={record.outcome === 'success' ? 'badge badge--published' : record.outcome === 'failure' ? 'badge badge--cancelled' : 'badge badge--draft'}>
                  {OUTCOMES[record.outcome] ?? record.outcome}
                </span>
                <time className="muted" dateTime={record.timestamp}>
                  {new Date(record.timestamp).toLocaleString('tr-TR')}
                </time>
              </div>
              {record.message && <p className="audit-row__message">{record.message}</p>}
              {record.changes && <p className="audit-row__changes">{record.changes}</p>}
              <div className="audit-row__meta muted">
                <span>{record.service}</span>
                {record.clientIp && <span>IP {record.clientIp}</span>}
                {record.actorRoles.length > 0 && <span>{record.actorRoles.join(', ')}</span>}
                {record.actorId && (
                  <button type="button" className="audit-row__actor" title="Bu kullanıcının kayıtlarını göster" disabled={loading} onClick={() => filterByActor(record.actorId!)}>
                    {record.actorId}
                  </button>
                )}
              </div>
              {Object.keys(record.target).length > 0 && (
                <dl className="audit-row__target">
                  {Object.entries(record.target).map(([key, value]) => (
                    <div key={key}>
                      <dt>{key}</dt>
                      <dd>{value}</dd>
                    </div>
                  ))}
                </dl>
              )}
            </li>
          ))}
        </ul>
      )}
      {result && (
        <Pagination page={result.page} pageSize={result.pageSize} totalCount={result.totalCount} disabled={loading} onPageChange={(page) => void load(shown, page)} />
      )}
    </div>
  )
}
