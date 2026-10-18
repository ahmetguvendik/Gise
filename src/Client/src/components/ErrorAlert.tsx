import type { ApiError } from '../api/http'

// Doğrulama hatalarında alan mesajları, iş kuralı hatalarında ayrıntı (ör. eksik bölümler) gösterilir.
export function ErrorAlert({ error }: { error: ApiError }) {
  const fieldMessages = Object.values(error.fieldErrors).flat()

  return (
    <div className="alert">
      <strong>{error.message}</strong>
      {fieldMessages.length > 0
        ? fieldMessages.map((message) => <div key={message}>{message}</div>)
        : error.detail && <div>{error.detail}</div>}
    </div>
  )
}
