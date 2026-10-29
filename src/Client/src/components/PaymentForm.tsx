import { useState } from 'react'
import type { FormEvent } from 'react'
import { ApiError, toApiError } from '../api/http'
import { formatPrice } from '../lib/format'
import type { PaymentRequest } from '../types'
import { ErrorAlert } from './ErrorAlert'

// Test sağlayıcısında sonucu kartın son dört hanesi belirler (bkz. tasarım 3.2); diğer sonlar başarılıdır.
const TEST_CARDS = [
  { last4: '0000', label: 'Başarılı' },
  { last4: '0002', label: 'Reddedilir' },
  { last4: '0005', label: 'Cevap gelmez' },
  { last4: '0009', label: 'Önce hata, sonra başarılı' },
]

interface Props {
  amount: number
  onSubmit: (card: PaymentRequest) => Promise<unknown>
}

// "4242424242420000" → "4242 4242 4242 0000"
const groupDigits = (value: string) =>
  value
    .replace(/\D/g, '')
    .slice(0, 16)
    .replace(/(\d{4})(?=\d)/g, '$1 ')

// "1230" → "12/30"
const formatExpiry = (value: string) => {
  const digits = value.replace(/\D/g, '').slice(0, 4)
  return digits.length > 2 ? `${digits.slice(0, 2)}/${digits.slice(2)}` : digits
}

// Kart bilgisi yalnızca bu bileşende durur; gönderildikten sonra sipariş sayfası formu kapatır.
export function PaymentForm({ amount, onSubmit }: Props) {
  const [cardNumber, setCardNumber] = useState('')
  const [expiry, setExpiry] = useState('')
  const [cvc, setCvc] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<ApiError | null>(null)

  const fillTestCard = (last4: string) => {
    setCardNumber(groupDigits(`424242424242${last4}`))
    setExpiry((current) => current || '12/30')
    setCvc((current) => current || '123')
    setError(null)
  }

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    const [month = '', year = ''] = expiry.split('/')
    setBusy(true)
    setError(null)
    try {
      await onSubmit({
        cardNumber: cardNumber.replace(/\s/g, ''),
        expiryMonth: Number(month),
        expiryYear: 2000 + Number(year),
        cvc,
      })
    } catch (err) {
      setError(toApiError(err))
      setBusy(false)
    }
  }

  return (
    <form className="form" onSubmit={submit}>
      <h2 className="form__title">Ödeme</h2>
      <p className="form__hint form__hint--warning">Bu bir test ödemesidir; gerçek kart bilgisi girmeyin.</p>

      <div className="test-cards">
        <span className="muted">Test kartı:</span>
        {TEST_CARDS.map((card) => (
          <button key={card.last4} type="button" className="test-card" onClick={() => fillTestCard(card.last4)} disabled={busy}>
            <strong>…{card.last4}</strong> {card.label}
          </button>
        ))}
      </div>

      {error && <ErrorAlert error={error} />}

      <label>
        Kart numarası
        <input
          value={cardNumber}
          onChange={(e) => setCardNumber(groupDigits(e.target.value))}
          inputMode="numeric"
          autoComplete="off"
          placeholder="4242 4242 4242 0000"
          maxLength={19}
          required
        />
      </label>
      <div className="form__row payment-form__row">
        <label>
          Son kullanma (AA/YY)
          <input
            value={expiry}
            onChange={(e) => setExpiry(formatExpiry(e.target.value))}
            inputMode="numeric"
            autoComplete="off"
            placeholder="12/30"
            maxLength={5}
            required
          />
        </label>
        <label>
          Güvenlik kodu
          <input
            value={cvc}
            onChange={(e) => setCvc(e.target.value.replace(/\D/g, '').slice(0, 3))}
            inputMode="numeric"
            autoComplete="off"
            placeholder="123"
            maxLength={3}
            required
          />
        </label>
      </div>

      <button className="button button--lg" disabled={busy}>
        {busy ? 'Gönderiliyor…' : `${formatPrice(amount)} öde`}
      </button>
    </form>
  )
}
