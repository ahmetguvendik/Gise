import { useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { useAuth } from 'react-oidc-context'
import { ApiError, toApiError } from '../api/http'
import { ticketsApi } from '../api/tickets'
import { ErrorAlert } from '../components/ErrorAlert'
import { formatLongDate, formatTime } from '../lib/format'
import type { CheckInResult, GateEvent } from '../types'

type Outcome = { kind: 'valid'; ticket: CheckInResult } | { kind: 'rejected'; error: ApiError }

// Reddedilen okutmanın görevliye gösterilen başlığı ve rengi; ayrıntı API'nin açıklamasında. Aynı HTTP durumunda birden
// çok sebep olabildiği için hata koduna bakılır.
const REJECTIONS: Record<string, { title: string; tone: 'used' | 'invalid' }> = {
  VALIDATION_FAILED: { title: 'Geçersiz kod', tone: 'invalid' },
  TICKET_NOT_FOUND: { title: 'Bilet bulunamadı', tone: 'invalid' },
  TICKET_ALREADY_USED: { title: 'Daha önce okutuldu', tone: 'used' },
  TICKET_CANCELLED: { title: 'Bilet iptal edilmiş', tone: 'invalid' },
  TICKET_WRONG_EVENT: { title: 'Başka etkinliğin bileti', tone: 'invalid' },
  TICKET_CHECKIN_NOT_OPEN: { title: 'Giriş henüz başlamadı', tone: 'used' },
  TICKET_CHECKIN_CLOSED: { title: 'Giriş süresi bitti', tone: 'used' },
}

// Cihaz bir kapıda bir etkinliğe bakar: seçim tarayıcıda hatırlanır. Okunamazsa (gizli pencere) her açılışta yeniden seçilir.
const EVENT_KEY = 'gise.gate.eventId'
const readSavedEvent = () => {
  try {
    return localStorage.getItem(EVENT_KEY)
  } catch {
    return null
  }
}
const saveEvent = (eventId: string) => {
  try {
    localStorage.setItem(EVENT_KEY, eventId)
  } catch {
    // Hatırlanmazsa görevli bir dahaki açılışta yeniden seçer.
  }
}

// Guard'ın arkasında: kapı görevlisi ya da admin. Önce kapıdaki etkinlik seçilir; yalnızca okutması açık etkinlikler
// listelenir (etkinlikten 4 saat önce açılır, başladıktan 2 saat sonra kapanır). QR okuyucu klavye gibi yazıp Enter'a
// basar; kod elle de girilebilir. Her okutmadan sonra alan temizlenir ve odakta kalır, sıradaki bilet hemen okutulur.
export function GatePage() {
  const auth = useAuth()
  const token = auth.user?.access_token ?? ''
  const inputRef = useRef<HTMLInputElement>(null)
  const [events, setEvents] = useState<GateEvent[] | null>(null)
  const [eventsError, setEventsError] = useState<ApiError | null>(null)
  const [eventId, setEventId] = useState<string | null>(readSavedEvent)
  const [code, setCode] = useState('')
  const [busy, setBusy] = useState(false)
  const [outcome, setOutcome] = useState<Outcome | null>(null)
  // Geçerli okutmadan sonra giriş sayısı yenilenir.
  const [version, setVersion] = useState(0)

  useEffect(() => {
    let ignore = false
    ticketsApi.gateEvents(token).then(
      (data) => {
        if (ignore) return
        setEvents(data)
        setEventsError(null)
      },
      (err) => {
        if (!ignore) setEventsError(toApiError(err))
      },
    )
    return () => {
      ignore = true
    }
  }, [token, version])

  // Hatırlanan etkinliğin kapısı kapandıysa seçim düşer; açık tek etkinlik varsa o seçilir.
  const selected =
    events?.find((e) => e.eventId === eventId) ?? (events?.length === 1 ? events[0] : undefined)

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    if (code.trim() === '' || !selected) return
    setBusy(true)
    try {
      setOutcome({ kind: 'valid', ticket: await ticketsApi.checkIn(code, selected.eventId, token) })
      setVersion((current) => current + 1)
    } catch (err) {
      setOutcome({ kind: 'rejected', error: toApiError(err) })
    } finally {
      setBusy(false)
      setCode('')
      inputRef.current?.focus()
    }
  }

  const choose = (id: string) => {
    setEventId(id)
    saveEvent(id)
    setOutcome(null)
  }

  const rejection = outcome?.kind === 'rejected' && outcome.error.code ? REJECTIONS[outcome.error.code] : undefined

  return (
    <div className="container page gate">
      <h1 className="page-title">Kapı</h1>

      {eventsError && <ErrorAlert error={eventsError} />}

      {events === null && !eventsError ? (
        <div className="skeleton skeleton--sm" />
      ) : events && events.length === 0 ? (
        <div className="card empty">
          <h2>Şu an girişi açık etkinlik yok</h2>
          <p className="muted">Kapılar etkinlikten 4 saat önce açılır, başladıktan 2 saat sonra kapanır.</p>
        </div>
      ) : (
        events && (
          <>
            <label className="gate__event">
              <span className="muted">Kapıdaki etkinlik</span>
              <select value={selected?.eventId ?? ''} onChange={(e) => choose(e.target.value)}>
                {!selected && <option value="">Etkinlik seçin</option>}
                {events.map((event) => (
                  <option key={event.eventId} value={event.eventId}>
                    {event.eventTitle} · {event.venueName} · {formatTime(event.eventStartsAt)}
                  </option>
                ))}
              </select>
            </label>

            {selected && (
              <p className="gate__stats">
                <strong>
                  {selected.checkedInCount} / {selected.ticketCount}
                </strong>{' '}
                giriş · kapılar {formatTime(selected.opensAt)}–{formatTime(selected.closesAt)} arası açık
              </p>
            )}

            <form className="form gate__form" onSubmit={submit}>
              <label>
                Bilet kodu
                <input
                  ref={inputRef}
                  value={code}
                  onChange={(e) => setCode(e.target.value)}
                  autoFocus
                  autoComplete="off"
                  autoCapitalize="characters"
                  spellCheck={false}
                  placeholder="ABCD-EFGH-JKMN-PQRS"
                  maxLength={40}
                  disabled={!selected}
                />
              </label>
              <button className="button button--lg" disabled={busy || code.trim() === '' || !selected}>
                {busy ? 'Kontrol ediliyor…' : 'Kontrol et'}
              </button>
            </form>
          </>
        )
      )}

      {outcome?.kind === 'valid' && (
        <div className="gate-result gate-result--valid" role="status">
          <strong className="gate-result__title">Geçerli</strong>
          <span className="gate-result__seat">
            {outcome.ticket.sectionName} · Sıra {outcome.ticket.row} · Koltuk {outcome.ticket.number}
          </span>
          <span>
            {outcome.ticket.eventTitle} · {formatLongDate(outcome.ticket.eventStartsAt)} {formatTime(outcome.ticket.eventStartsAt)}
          </span>
        </div>
      )}

      {outcome?.kind === 'rejected' &&
        (rejection ? (
          <div className={`gate-result gate-result--${rejection.tone}`} role="alert">
            <strong className="gate-result__title">{rejection.title}</strong>
            <span>{Object.values(outcome.error.fieldErrors).flat()[0] ?? outcome.error.detail ?? outcome.error.message}</span>
          </div>
        ) : (
          <ErrorAlert error={outcome.error} />
        ))}
    </div>
  )
}
