import { useEffect, useRef, useState } from 'react'
import { ApiError, toApiError } from '../api/http'
import { eventsApi } from '../api/events'
import { mediaApi } from '../api/media'
import type { EventDetail, MediaPolicy } from '../types'
import { ErrorAlert } from './ErrorAlert'
import { Poster } from './Poster'

interface Props {
  event: EventDetail
  token: string
  onSaved: () => void
}

const typeLabel = (contentType: string) => contentType.replace('image/', '').toUpperCase()

// Dosya seçilince hemen yüklenir ve etkinliğe bağlanır; ayrı bir kaydet düğmesi yok.
export function PosterForm({ event, token, onSaved }: Props) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [policy, setPolicy] = useState<MediaPolicy | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<ApiError | null>(null)
  const [notice, setNotice] = useState<string | null>(null)

  useEffect(() => {
    let ignore = false
    mediaApi.policy().then(
      (data) => {
        if (!ignore) setPolicy(data)
      },
      () => {
        if (!ignore) setPolicy(null)
      },
    )
    return () => {
      ignore = true
    }
  }, [])

  const maxMb = policy ? Math.round(policy.maxSizeBytes / (1024 * 1024)) : null

  const save = async (action: () => Promise<unknown>, message: string) => {
    setBusy(true)
    setError(null)
    setNotice(null)
    try {
      await action()
      setNotice(message)
      onSaved()
    } catch (err) {
      setError(toApiError(err))
    } finally {
      setBusy(false)
      if (inputRef.current) inputRef.current.value = ''
    }
  }

  const choose = (file: File) => {
    // Sunucu yine doğrular; bu yalnızca boşuna yüklemeyi önler.
    if (policy && !policy.allowedContentTypes.includes(file.type)) {
      setError(new ApiError(0, 'Bu dosya türü yüklenemez.', {}, `İzin verilen türler: ${policy.allowedContentTypes.map(typeLabel).join(', ')}.`))
      return
    }
    if (policy && file.size > policy.maxSizeBytes) {
      setError(new ApiError(0, 'Dosya boyutu sınırı aşıyor.', {}, `En fazla ${maxMb} MB yükleyebilirsiniz.`))
      return
    }
    void save(async () => {
      const { key } = await mediaApi.upload(file, token)
      await eventsApi.setPoster(event.id, key, token)
    }, 'Afiş kaydedildi.')
  }

  const remove = () => {
    if (!window.confirm('Afiş kaldırılsın mı? Etkinlik renkli yer tutucuyla görünür.')) return
    void save(() => eventsApi.setPoster(event.id, null, token), 'Afiş kaldırıldı.')
  }

  return (
    <div className="manage__block poster-form">
      <h3>Afiş</h3>

      {error && <ErrorAlert error={error} />}
      {notice && <div className="notice">{notice}</div>}

      <div className="poster-form__body">
        <div className="poster-form__preview">
          <Poster
            id={event.id}
            title={event.title}
            startsAt={event.startsAt}
            imageUrl={event.posterUrl}
            imageSrcset={event.posterSrcset}
          />
        </div>
        <div className="poster-form__actions">
          <p className="muted">
            Yatay, 3:2 oranında bir görsel önerilir (ör. 1500×1000); listedeki kartlarda kenarları biraz kırpılabilir.
            {policy && ` ${policy.allowedContentTypes.map(typeLabel).join(', ')} · en fazla ${maxMb} MB.`}
          </p>
          <button type="button" className="button button--contrast" onClick={() => inputRef.current?.click()} disabled={busy}>
            {busy ? 'Yükleniyor…' : event.posterUrl ? 'Afişi değiştir' : 'Afiş yükle'}
          </button>
          {event.posterUrl && (
            <button type="button" className="button button--ghost" onClick={remove} disabled={busy}>
              Afişi kaldır
            </button>
          )}
          <input
            ref={inputRef}
            type="file"
            hidden
            accept={policy?.allowedContentTypes.join(',')}
            onChange={(e) => {
              const file = e.target.files?.[0]
              if (file) choose(file)
            }}
          />
        </div>
      </div>
    </div>
  )
}
