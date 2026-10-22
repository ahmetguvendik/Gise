import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { ApiError, toApiError } from '../api/http'
import { eventsApi } from '../api/events'
import type { EventDetail, VenueDetail } from '../types'
import { ErrorAlert } from './ErrorAlert'
import { EventInfoForm } from './EventInfoForm'
import { PriceForm } from './PriceForm'
import { ReasonForm } from './ReasonForm'

interface Props {
  event: EventDetail
  venue: VenueDetail
  token: string
  admin: boolean
  /** Durum değişti: sayfa veriyi yeniden yükler ve mesajı üstte gösterir. */
  onChanged: (message: string) => void
  /** Bilgi ya da fiyat kaydedildi: sayfa veriyi yeniden yükler. */
  onSaved: () => void
}

// Organizatör taslak hazırlar ve onaya gönderir; yayınlama, yayındaki etkinliği değiştirme ve iptal admin'in işi.
export function EventManage({ event, venue, token, admin, onChanged, onSaved }: Props) {
  const navigate = useNavigate()
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<ApiError | null>(null)

  const unpriced = venue.sections.filter((s) => !event.prices.some((p) => p.sectionId === s.id))
  const unpricedText = `Her bölümün fiyatı girilmeli. Eksik: ${unpriced.map((s) => s.name).join(', ')}.`
  // Başlamış etkinlikte API değişikliği reddeder; formlar hiç gösterilmez.
  const started = new Date(event.startsAt) <= new Date()
  // Taslağı sahibi ve admin, yayındakini yalnızca admin değiştirir; onay bekleyen değişmez.
  const editable = event.status === 'Draft' || (event.status === 'Published' && admin)

  const run = async (action: () => Promise<unknown>, message: string) => {
    setBusy(true)
    setError(null)
    try {
      await action()
      onChanged(message)
    } catch (err) {
      setError(toApiError(err))
    } finally {
      setBusy(false)
    }
  }

  const publish = () => run(() => eventsApi.publish(event.id, token), 'Etkinlik yayınlandı; artık herkese açık listede görünüyor.')

  const remove = async () => {
    if (!window.confirm(`"${event.title}" taslağı fiyatlarıyla birlikte silinsin mi? Bu geri alınamaz.`)) return
    setBusy(true)
    setError(null)
    try {
      await eventsApi.remove(event.id, token)
      navigate('/etkinliklerim')
    } catch (err) {
      setError(toApiError(err))
      setBusy(false)
    }
  }

  return (
    <section className="card card--dashed manage" aria-labelledby="yonetim">
      <div className="manage__head">
        <h2 id="yonetim" className="manage__title">
          Yönetim
        </h2>
        <span className="muted">Yalnızca etkinliğin sahibi ve yönetici görür</span>
      </div>

      {error && <ErrorAlert error={error} />}

      {started ? (
        <p className="muted">Etkinlik başladı; artık değişiklik yapılamaz.</p>
      ) : (
        <>
          {event.status === 'Draft' && event.returnReason && (
            <div className="review-note">
              <strong>Admin taslağa geri gönderdi</strong>
              <p>{event.returnReason}</p>
            </div>
          )}

          {event.status === 'Published' && !admin && (
            <p className="muted">Etkinlik yayında. Tarih ya da fiyat değişikliği ve iptal için yöneticiyle iletişime geçin.</p>
          )}

          {editable && (
            <>
              <EventInfoForm event={event} venueName={`${venue.name}, ${venue.city}`} token={token} onSaved={onSaved} />

              {/* Mekân değişince bölümler değişir: form yeni bölümlerle baştan kurulur. */}
              <PriceForm
                key={venue.id}
                eventId={event.id}
                token={token}
                sections={venue.sections}
                prices={event.prices}
                onSaved={onSaved}
              />
            </>
          )}

          {event.status === 'Draft' &&
            (admin ? (
              <div className="manage__block">
                <h3>Yayınla</h3>
                <p className="muted">
                  {unpriced.length > 0 ? unpricedText : 'Etkinlik herkese açık listeye düşer ve satışa açılır.'}
                </p>
                <button className="button button--lg" onClick={() => void publish()} disabled={busy || unpriced.length > 0}>
                  {busy ? 'Yayınlanıyor…' : 'Yayınla'}
                </button>
              </div>
            ) : (
              <div className="manage__block">
                <h3>Onaya gönder</h3>
                <p className="muted">
                  {unpriced.length > 0
                    ? unpricedText
                    : 'Yönetici inceleyip yayınlar. Onay beklerken değişiklik yapamazsınız; gerekirse geri çekebilirsiniz.'}
                </p>
                <button
                  className="button button--lg"
                  onClick={() => void run(() => eventsApi.submit(event.id, token), 'Etkinlik onaya gönderildi; yönetici inceleyince yayınlanır.')}
                  disabled={busy || unpriced.length > 0}
                >
                  {busy ? 'Gönderiliyor…' : 'Onaya gönder'}
                </button>
              </div>
            ))}

          {event.status === 'PendingReview' &&
            (admin ? (
              <>
                <div className="manage__block">
                  <h3>Yayınla</h3>
                  <p className="muted">Organizatör onaya gönderdi. Yukarıdaki bilgileri ve fiyatları kontrol edin.</p>
                  <button className="button button--lg" onClick={() => void publish()} disabled={busy}>
                    {busy ? 'Yayınlanıyor…' : 'Yayınla'}
                  </button>
                </div>
                <ReasonForm
                  title="Taslağa geri gönder"
                  description="Organizatör sebebi görür, düzeltip yeniden gönderir."
                  label="Sebep"
                  submitLabel="Taslağa geri gönder"
                  busyLabel="Gönderiliyor…"
                  onSubmit={(reason) => eventsApi.returnToDraft(event.id, reason, token)}
                  onDone={() => onChanged('Etkinlik organizatöre geri gönderildi.')}
                />
              </>
            ) : (
              <div className="manage__block">
                <h3>Onay bekliyor</h3>
                <p className="muted">
                  Yönetici inceleyince yayınlanır ya da notla taslağa geri gelir. Değişiklik yapmak için onaydan geri çekin.
                </p>
                <button
                  className="button button--outline"
                  onClick={() => void run(() => eventsApi.withdraw(event.id, token), 'Etkinlik onaydan geri çekildi; yeniden düzenleyebilirsiniz.')}
                  disabled={busy}
                >
                  Onaydan geri çek
                </button>
              </div>
            ))}

          {event.status === 'Draft' && (
            <div className="manage__danger">
              <h3>Taslağı sil</h3>
              <p className="muted">Taslak henüz kimseye görünmüyor; silinince fiyatlarıyla birlikte kalkar.</p>
              <button className="button button--danger-outline" onClick={() => void remove()} disabled={busy}>
                Taslağı sil
              </button>
            </div>
          )}

          {event.status === 'Published' && admin && (
            <ReasonForm
              danger
              title="Etkinliği iptal et"
              description='İptal geri alınamaz. Etkinlik listeden kalkar; sayfası sebebiyle birlikte "iptal edildi" olarak kalır.'
              label="İptal sebebi"
              submitLabel="Etkinliği iptal et"
              busyLabel="İptal ediliyor…"
              confirm={`"${event.title}" iptal edilsin mi? Bu geri alınamaz; etkinlik satıştan ve listeden kalkar.`}
              onSubmit={(reason) => eventsApi.cancel(event.id, reason, token)}
              onDone={() => onChanged('Etkinlik iptal edildi.')}
            />
          )}
        </>
      )}
    </section>
  )
}
