import { useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { useAuth } from 'react-oidc-context'
import { ApiError, toApiError } from '../api/http'
import { ticketsApi } from '../api/tickets'
import { ErrorAlert } from '../components/ErrorAlert'
import { formatLongDate, formatTime } from '../lib/format'
import type { CheckInResult } from '../types'

type Outcome = { kind: 'valid'; ticket: CheckInResult } | { kind: 'rejected'; error: ApiError }

// Reddedilen okutmanın görevliye gösterilen başlığı; ayrıntı API'nin açıklamasında.
const REJECTIONS: Record<number, string> = {
  400: 'Geçersiz kod',
  404: 'Bilet bulunamadı',
  409: 'Daha önce okutuldu',
  422: 'Bilet iptal edilmiş',
}

// Guard'ın arkasında: kapı görevlisi ya da admin. QR okuyucu klavye gibi yazıp Enter'a basar; kod elle de girilebilir.
// Her okutmadan sonra alan temizlenir ve odakta kalır, sıradaki bilet hemen okutulur.
export function GatePage() {
  const auth = useAuth()
  const token = auth.user?.access_token ?? ''
  const inputRef = useRef<HTMLInputElement>(null)
  const [code, setCode] = useState('')
  const [busy, setBusy] = useState(false)
  const [outcome, setOutcome] = useState<Outcome | null>(null)

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    if (code.trim() === '') return
    setBusy(true)
    try {
      setOutcome({ kind: 'valid', ticket: await ticketsApi.checkIn(code, token) })
    } catch (err) {
      setOutcome({ kind: 'rejected', error: toApiError(err) })
    } finally {
      setBusy(false)
      setCode('')
      inputRef.current?.focus()
    }
  }

  const rejection = outcome?.kind === 'rejected' ? REJECTIONS[outcome.error.status] : undefined

  return (
    <div className="container page gate">
      <h1 className="page-title">Kapı</h1>
      <p className="muted">QR'ı okutun ya da biletteki kodu yazıp Enter'a basın.</p>

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
          />
        </label>
        <button className="button button--lg" disabled={busy || code.trim() === ''}>
          {busy ? 'Kontrol ediliyor…' : 'Kontrol et'}
        </button>
      </form>

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
          <div className={`gate-result ${outcome.error.status === 409 ? 'gate-result--used' : 'gate-result--invalid'}`} role="alert">
            <strong className="gate-result__title">{rejection}</strong>
            <span>{Object.values(outcome.error.fieldErrors).flat()[0] ?? outcome.error.detail ?? outcome.error.message}</span>
          </div>
        ) : (
          <ErrorAlert error={outcome.error} />
        ))}
    </div>
  )
}
