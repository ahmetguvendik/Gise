import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { toApiError } from '../api/http'
import { venuesApi } from '../api/venues'
import type { VenueSummary } from '../types'

// Guard'ın arkasında: yalnızca admin.
export function VenuesPage() {
  const [venues, setVenues] = useState<VenueSummary[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let ignore = false
    venuesApi.list().then(
      (data) => {
        if (!ignore) setVenues(data)
      },
      (err) => {
        if (!ignore) setError(toApiError(err).message)
      },
    )
    return () => {
      ignore = true
    }
  }, [])

  return (
    <div className="container page">
      <div className="section-head">
        <div className="stack">
          <p className="eyebrow">Yönetim</p>
          <h1 className="page-title">Mekânlar</h1>
        </div>
        <Link className="button button--lg" to="/mekan/yeni">
          + Yeni mekân
        </Link>
      </div>

      {error && <div className="alert">{error}</div>}

      {venues === null && !error ? (
        <ul className="venue-grid" aria-label="Yükleniyor">
          <li className="skeleton skeleton--sm" />
          <li className="skeleton skeleton--sm" />
          <li className="skeleton skeleton--sm" />
        </ul>
      ) : venues && venues.length === 0 ? (
        <div className="card empty">
          <h2>Henüz mekân yok</h2>
          <p className="muted">Etkinlik açılabilmesi için önce bir mekân ekleyin.</p>
        </div>
      ) : (
        venues && (
          <ul className="venue-grid">
            {venues.map((venue) => (
              <li key={venue.id} className="venue-card">
                <div className="venue-card__head">
                  <h2 className="venue-card__name">
                    <Link to={`/mekan/${venue.id}/duzenle`}>{venue.name}</Link>
                  </h2>
                  {venue.hasEvents && <span className="badge badge--published">Kullanımda</span>}
                </div>
                <p className="muted">{venue.city}</p>
                <div className="perforation" />
                <p className="venue-card__stats">
                  <strong>{venue.sectionCount}</strong> bölüm · <strong>{venue.seatCount}</strong> koltuk
                </p>
              </li>
            ))}
          </ul>
        )
      )}
    </div>
  )
}
