import { Link } from 'react-router-dom'

export function NotFoundPage() {
  return (
    <div className="container page">
      <div className="card empty">
        <h1>Sayfa bulunamadı</h1>
        <p className="muted">Aradığınız adres yok ya da taşınmış.</p>
        <Link className="button" to="/">
          Etkinliklere dön
        </Link>
      </div>
    </div>
  )
}
