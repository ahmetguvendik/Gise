import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useAuth } from 'react-oidc-context'
import { ApiError, toApiError } from '../api/http'
import { venuesApi } from '../api/venues'
import { ErrorAlert } from '../components/ErrorAlert'
import { LayoutEditor } from '../components/LayoutEditor'
import { SeatPreview } from '../components/SeatPreview'
import type { VenueSectionInput } from '../types'

const emptyLayout = (): VenueSectionInput[] => [{ name: '', rows: [{ row: '1', seatCount: 10 }] }]

// API alan hatalarını "sections[0].rows[1].seatCount" diye döner; kullanıcıya hangi bölüm ve sıra olduğu söylenir.
function withLayoutNames(error: ApiError, sections: VenueSectionInput[]) {
  const fieldErrors = Object.fromEntries(
    Object.entries(error.fieldErrors).map(([key, messages]) => {
      const match = /^sections\[(\d+)\](?:\.rows\[(\d+)\])?/.exec(key)
      if (!match) return [key, messages]
      const section = sections[Number(match[1])]
      const sectionName = section?.name || `${Number(match[1]) + 1}. bölüm`
      const rowName = match[2] !== undefined ? section?.rows[Number(match[2])]?.row : undefined
      const where = rowName !== undefined ? `${sectionName} · ${rowName}. sıra` : sectionName
      return [key, messages.map((message) => `${where}: ${message}`)]
    }),
  )
  return new ApiError(error.status, error.message, fieldErrors, error.detail)
}

// Guard'ın arkasında: yalnızca admin. /mekan/yeni ve /mekan/:id/duzenle aynı sayfa.
export function VenueEditorPage() {
  const { id } = useParams()
  const isNew = id === undefined
  const auth = useAuth()
  const token = auth.user?.access_token ?? ''
  const navigate = useNavigate()

  const [name, setName] = useState('')
  const [city, setCity] = useState('')
  const [sections, setSections] = useState<VenueSectionInput[]>(emptyLayout)
  const [savedLayout, setSavedLayout] = useState('')
  const [hasEvents, setHasEvents] = useState(false)
  const [loaded, setLoaded] = useState(isNew)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<ApiError | null>(null)
  const [saved, setSaved] = useState(false)

  useEffect(() => {
    if (isNew) return
    let ignore = false
    venuesApi.get(id).then(
      (venue) => {
        if (ignore) return
        const layout = venue.sections.map((s) => ({ name: s.name, rows: s.rows.map((r) => ({ row: r.row, seatCount: r.seatCount })) }))
        setName(venue.name)
        setCity(venue.city)
        setSections(layout)
        setSavedLayout(JSON.stringify(layout))
        setHasEvents(venue.hasEvents)
        setLoaded(true)
      },
      (err) => {
        if (!ignore) setError(toApiError(err))
      },
    )
    return () => {
      ignore = true
    }
  }, [id, isNew])

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setBusy(true)
    setError(null)
    setSaved(false)
    try {
      if (isNew) {
        await venuesApi.create({ name, city, sections }, token)
        navigate('/mekanlar')
        return
      }
      await venuesApi.update(id, { name, city }, token)
      // Düzen yalnızca değiştiyse ve mekânda etkinlik yoksa gönderilir.
      if (!hasEvents && JSON.stringify(sections) !== savedLayout) {
        await venuesApi.updateLayout(id, sections, token)
        setSavedLayout(JSON.stringify(sections))
      }
      setSaved(true)
    } catch (err) {
      setError(withLayoutNames(toApiError(err), sections))
    } finally {
      setBusy(false)
    }
  }

  const remove = async () => {
    if (isNew || !window.confirm(`"${name}" mekânı bölümleri ve koltuklarıyla silinsin mi? Bu geri alınamaz.`)) return
    setBusy(true)
    setError(null)
    try {
      await venuesApi.remove(id, token)
      navigate('/mekanlar')
    } catch (err) {
      setError(toApiError(err))
      setBusy(false)
    }
  }

  if (!loaded) {
    return <div className="container page">{error ? <ErrorAlert error={error} /> : <div className="skeleton" />}</div>
  }

  return (
    <div className="container page">
      <Link className="back-link" to="/mekanlar">
        ← Mekânlar
      </Link>
      <h1 className="page-title">{isNew ? 'Yeni mekân' : name || 'Mekân'}</h1>

      <form className="editor-grid" onSubmit={submit}>
        <div className="form">
          {error && <ErrorAlert error={error} />}
          {saved && <div className="notice">Mekân kaydedildi.</div>}

          <div className="card form">
            <h2 className="form__title">Bilgiler</h2>
            <div className="form__row">
              <label>
                Mekân adı
                <input value={name} onChange={(e) => setName(e.target.value)} maxLength={200} required />
              </label>
              <label>
                Şehir
                <input value={city} onChange={(e) => setCity(e.target.value)} maxLength={100} required />
              </label>
            </div>
          </div>

          <div className="card form">
            <h2 className="form__title">Bölümler ve sıralar</h2>
            {hasEvents && (
              <p className="muted">
                Bu mekânda etkinlik olduğu için bölüm düzeni değiştirilemez; yalnızca ad ve şehir güncellenebilir.
              </p>
            )}
            <LayoutEditor sections={sections} onChange={setSections} disabled={hasEvents} />
          </div>

          <div className="editor-actions">
            <button className="button button--lg" disabled={busy}>
              {busy ? 'Kaydediliyor…' : isNew ? 'Mekânı oluştur' : 'Kaydet'}
            </button>
            {!isNew && !hasEvents && (
              <button type="button" className="button button--ghost" onClick={() => void remove()} disabled={busy}>
                Mekânı sil
              </button>
            )}
          </div>
        </div>

        <aside className="editor-grid__aside">
          <SeatPreview sections={sections} />
        </aside>
      </form>
    </div>
  )
}
