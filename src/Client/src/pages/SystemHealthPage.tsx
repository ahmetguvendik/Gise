import { useEffect, useState } from 'react'
import { useAuth } from 'react-oidc-context'
import { ApiError, toApiError } from '../api/http'
import { systemApi } from '../api/system'
import { ErrorAlert } from '../components/ErrorAlert'
import type { HealthStatus, SystemHealth, SystemHealthItem } from '../types'

// Panel servisleri 30 sn'de bir yoklar; sayfa ondan sık sormaz ama kullanıcı bekletilmesin diye 15 sn'de bir yenilenir.
const REFRESH_MS = 15_000

const STATUS_LABELS: Record<HealthStatus, string> = {
  Healthy: 'Sağlıklı',
  Degraded: 'Uyarı',
  Unhealthy: 'Sağlıksız',
  Unknown: 'Bilinmiyor',
}

const SUMMARY: Record<HealthStatus, string> = {
  Healthy: 'Her şey çalışıyor.',
  Degraded: 'Sistem çalışıyor ama bazı bileşenlerde uyarı var.',
  Unhealthy: 'Sağlıksız bileşen var.',
  Unknown: 'Henüz kontrol edilmedi; birkaç saniye sonra yenilenir.',
}

const ago = (iso: string | null) => {
  if (!iso) return 'henüz yok'
  const seconds = Math.max(0, Math.round((Date.now() - new Date(iso).getTime()) / 1000))
  if (seconds < 60) return `${seconds} sn önce`
  if (seconds < 3600) return `${Math.floor(seconds / 60)} dk önce`
  if (seconds < 86_400) return `${Math.floor(seconds / 3600)} sa önce`
  return `${Math.floor(seconds / 86_400)} gün önce`
}

function StatusBadge({ status }: { status: HealthStatus }) {
  return <span className={`health-badge health-badge--${status.toLowerCase()}`}>{STATUS_LABELS[status]}</span>
}

function Item({ item }: { item: SystemHealthItem }) {
  const failing = item.checks.filter((check) => check.status !== 'Healthy')
  return (
    // Sorunlu kalem açık gelir: nedeni göstermek için tıklamak gerekmesin.
    <details className="health-item" open={failing.length > 0}>
      <summary className="health-item__head">
        <span className={`health-dot health-dot--${item.status.toLowerCase()}`} aria-hidden="true" />
        <span className="health-item__name">{item.name}</span>
        <StatusBadge status={item.status} />
        <span className="health-item__when muted">{item.status === 'Healthy' ? `kontrol: ${ago(item.lastExecuted)}` : `sorun ${ago(item.onStateFrom)} başladı`}</span>
      </summary>
      <ul className="health-checks">
        {item.checks.length === 0 && <li className="muted">Ayrıntı yok.</li>}
        {item.checks.map((check) => (
          <li key={check.name} className="health-check">
            <span className={`health-dot health-dot--${check.status.toLowerCase()}`} aria-hidden="true" />
            <strong>{check.name}</strong>
            <span className="muted">{STATUS_LABELS[check.status]}</span>
            {check.description && <span className="health-check__text">{check.description}</span>}
          </li>
        ))}
      </ul>
    </details>
  )
}

// Guard'ın arkasında: yalnızca admin. healthcheck-api'nin son yoklama sonuçları; sorunlu kalemler açık gelir.
export function SystemHealthPage() {
  const auth = useAuth()
  const token = auth.user?.access_token ?? ''
  const [health, setHealth] = useState<SystemHealth | null>(null)
  const [error, setError] = useState<ApiError | null>(null)
  // Her yenileme isteği (düğme ya da zamanlayıcı) sürümü artırır; son yüklenen sürüm geride kalıyorsa yenileniyor demektir.
  const [version, setVersion] = useState(0)
  const [loadedVersion, setLoadedVersion] = useState(-1)

  useEffect(() => {
    let ignore = false
    systemApi.health(token).then(
      (data) => {
        if (ignore) return
        setHealth(data)
        setError(null)
        setLoadedVersion(version)
      },
      (err) => {
        if (ignore) return
        setError(toApiError(err))
        setLoadedVersion(version)
      },
    )
    return () => {
      ignore = true
    }
  }, [token, version])

  // Zamanlayıcı yenilemeyi de, "x sn önce" metinlerinin tazelenmesini de sağlar (sürüm değişince sayfa yeniden çizilir).
  useEffect(() => {
    const timer = setInterval(() => setVersion((current) => current + 1), REFRESH_MS)
    return () => clearInterval(timer)
  }, [])

  const loading = loadedVersion !== version

  const items = health?.groups.flatMap((group) => group.items) ?? []
  const count = (status: HealthStatus) => items.filter((item) => item.status === status).length

  return (
    <div className="container page">
      <div className="stack">
        <p className="eyebrow">Yönetici</p>
        <h1 className="page-title">Sistem durumu</h1>
        <p className="muted">Servislerin ve altyapının son kontrol sonuçları. Bir bileşen düşünce ya da düzelince Slack'e de yazılır.</p>
      </div>

      {error && <ErrorAlert error={error} />}

      {health === null && !error && <div className="skeleton skeleton--sm" />}

      {health && (
        <>
          <section className={`card health-summary health-summary--${health.status.toLowerCase()}`} aria-live="polite">
            <div>
              <StatusBadge status={health.status} />
              <p className="health-summary__text">{SUMMARY[health.status]}</p>
              <p className="muted">
                {count('Healthy')} sağlıklı · {count('Degraded')} uyarı · {count('Unhealthy')} sağlıksız
                {count('Unknown') > 0 && ` · ${count('Unknown')} bilinmiyor`}
              </p>
            </div>
            <button type="button" className="button button--outline" disabled={loading} onClick={() => setVersion((current) => current + 1)}>
              {loading ? 'Yenileniyor…' : 'Yenile'}
            </button>
          </section>

          {health.groups.map((group) => (
            <section key={group.name} className="health-group" aria-label={group.name}>
              <h2>{group.name}</h2>
              <div className="card health-list">
                {group.items.map((item) => (
                  <Item key={item.name} item={item} />
                ))}
              </div>
            </section>
          ))}
        </>
      )}
    </div>
  )
}
