import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { toApiError } from '../api/http'
import { searchApi } from '../api/search'
import { EventCard } from '../components/EventCard'
import { Poster } from '../components/Poster'
import { SearchBox } from '../components/SearchBox'
import { DATE_PRESETS, dateRange, todayValue } from '../lib/dateRange'
import { formatPrice, formatWeekdayTime } from '../lib/format'
import type { CityFacet, SearchEventItem } from '../types'

const PAGE_SIZE = 24

interface Results {
  /** Hangi filtreye ait; filtre değişince eski sonuç yeni yüklenene kadar soluk gösterilir. */
  key: string
  items: SearchEventItem[]
  total: number
  page: number
  cities: CityFacet[]
}

// Ana sayfa: liste, arama ve filtreler Search servisinden. Filtreler adres çubuğunda durur; paylaşılabilir,
// geri tuşu önceki aramaya döner.
export function EventsPage() {
  const [params, setParams] = useSearchParams()
  const q = params.get('q') ?? ''
  const city = params.get('sehir') ?? ''
  const preset = params.get('tarih') ?? ''
  const start = params.get('bas') ?? ''
  const end = params.get('bit') ?? ''
  const key = JSON.stringify([q, city, preset, start, end])
  const filtered = q !== '' || city !== '' || preset !== ''

  const [results, setResults] = useState<Results | null>(null)
  const [loadingMore, setLoadingMore] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const criteria = (page: number) => ({ q, city, ...dateRange(preset, start, end), page, pageSize: PAGE_SIZE })

  useEffect(() => {
    // Geç gelen eski bir yanıt, yenisinin üzerine yazmasın.
    let ignore = false
    searchApi.events({ q, city, ...dateRange(preset, start, end), page: 1, pageSize: PAGE_SIZE }).then(
      (data) => {
        if (ignore) return
        setResults({ key: JSON.stringify([q, city, preset, start, end]), ...data })
        setError(null)
      },
      (err) => {
        if (!ignore) setError(toApiError(err).message)
      },
    )
    return () => {
      ignore = true
    }
  }, [q, city, preset, start, end])

  const loadMore = () => {
    if (!results) return
    setLoadingMore(true)
    searchApi
      .events(criteria(results.page + 1))
      .then(
        (data) =>
          setResults((current) => {
            if (!current || current.key !== key) return current
            // Sayfalar arasında yeni etkinlik yayınlanırsa kayan kayıt iki kez görünmesin.
            const seen = new Set(current.items.map((item) => item.id))
            return { ...current, items: [...current.items, ...data.items.filter((item) => !seen.has(item.id))], total: data.total, page: data.page }
          }),
        (err) => setError(toApiError(err).message),
      )
      .finally(() => setLoadingMore(false))
  }

  // Boş değer parametreyi kaldırır.
  const update = (changes: Record<string, string>, replace = false) =>
    setParams(
      (current) => {
        const next = new URLSearchParams(current)
        for (const [name, value] of Object.entries(changes)) {
          if (value) next.set(name, value)
          else next.delete(name)
        }
        return next
      },
      { replace },
    )

  const showResults = () => document.getElementById('etkinlikler')?.scrollIntoView({ behavior: 'smooth', block: 'start' })

  const stale = results !== null && results.key !== key
  // Filtresiz listede en yakın etkinlik öne çıkar.
  const featured = !filtered && !stale ? results?.items[0] : undefined
  // Seçili şehir bu aramada hiç geçmiyorsa da listede kalsın; yoksa seçim görünmez olur.
  const cityOptions = results?.cities ?? []
  const cities = city && !cityOptions.some((option) => option.name === city) ? [{ name: city, count: 0 }, ...cityOptions] : cityOptions
  const today = todayValue()

  return (
    <>
      <section className="hero">
        <div className="container hero__inner">
          <div className="hero__copy">
            <p className="eyebrow">Biletin cebinde</p>
            <h1 className="hero__title">Bu akşam nereye?</h1>
            <p className="hero__lead">Konser, tiyatro, gösteri: yaklaşan etkinlikler tek yerde.</p>
            <SearchBox
              value={q}
              onSearch={(text) => {
                update({ q: text })
                showResults()
              }}
              onCity={(name) => {
                update({ q: '', sehir: name })
                showResults()
              }}
            />
          </div>

          {featured && (
            <article className="featured">
              <Poster
                id={featured.id}
                title={featured.title}
                startsAt={featured.startsAt}
                imageUrl={featured.posterUrl}
                imageSrcset={featured.posterSrcset}
              />
              <div className="perforation" />
              <div className="featured__body">
                <h2 className="featured__title">
                  <Link to={`/etkinlik/${featured.id}`}>{featured.title}</Link>
                </h2>
                <p className="muted">
                  {featured.venueName}, {featured.city} · {formatWeekdayTime(featured.startsAt)}
                </p>
                <p className="price-lead">
                  <strong>{formatPrice(featured.minPrice)}</strong>'den başlayan fiyatlarla
                </p>
              </div>
            </article>
          )}
        </div>
      </section>

      <div id="etkinlikler" className="container page results">
        <div className="stack">
          <div className="section-head">
            <h2 className="section-title">{q ? `“${q}” için sonuçlar` : filtered ? 'Etkinlikler' : 'Yaklaşan etkinlikler'}</h2>
            {results && !stale && (
              <p className="muted">
                {results.total} etkinlik · {q ? 'en uygun önce' : 'tarihe göre sıralı'}
              </p>
            )}
          </div>

          <div className="filters" role="group" aria-label="Filtreler">
            <label>
              <span className="visually-hidden">Şehir</span>
              <select value={city} onChange={(event) => update({ sehir: event.target.value })}>
                <option value="">Tüm şehirler</option>
                {cities.map((option) => (
                  <option key={option.name} value={option.name}>
                    {option.name} ({option.count})
                  </option>
                ))}
              </select>
            </label>

            <div className="chips">
              <button type="button" className="chip" aria-pressed={preset === ''} onClick={() => update({ tarih: '', bas: '', bit: '' })}>
                Tüm tarihler
              </button>
              {DATE_PRESETS.map((option) => (
                <button
                  key={option.value}
                  type="button"
                  className="chip"
                  aria-pressed={preset === option.value}
                  onClick={() => update(option.value === 'aralik' ? { tarih: option.value } : { tarih: option.value, bas: '', bit: '' })}
                >
                  {option.label}
                </button>
              ))}
            </div>

            {preset === 'aralik' && (
              <div className="filters__range">
                <label>
                  Başlangıç
                  <input type="date" value={start} min={today} max={end || undefined} onChange={(event) => update({ bas: event.target.value }, true)} />
                </label>
                <label>
                  Bitiş
                  <input type="date" value={end} min={start || today} onChange={(event) => update({ bit: event.target.value }, true)} />
                </label>
              </div>
            )}

            {filtered && (
              <button type="button" className="button button--ghost" onClick={() => setParams({})}>
                Filtreleri temizle
              </button>
            )}
          </div>
        </div>

        {error && <div className="alert">{error}</div>}

        {results === null && !error ? (
          <ul className="event-grid" aria-label="Yükleniyor">
            <li className="skeleton" />
            <li className="skeleton" />
            <li className="skeleton" />
          </ul>
        ) : results && results.items.length === 0 ? (
          filtered ? (
            <div className={`card empty${stale ? ' is-loading' : ''}`}>
              <h3>Aramanıza uyan etkinlik yok</h3>
              <p className="muted">Başka bir kelime deneyin ya da filtreleri kaldırın.</p>
              <button type="button" className="button button--outline" onClick={() => setParams({})}>
                Filtreleri temizle
              </button>
            </div>
          ) : (
            <div className="card empty">
              <h3>Şu an yaklaşan etkinlik yok</h3>
              <p className="muted">Yeni etkinlikler yayınlandıkça burada görünecek.</p>
            </div>
          )
        ) : (
          results && (
            <>
              <ul className={`event-grid${stale ? ' is-loading' : ''}`} aria-busy={stale}>
                {results.items.map((event) => (
                  <EventCard
                    key={event.id}
                    id={event.id}
                    title={event.title}
                    startsAt={event.startsAt}
                    venue={`${event.venueName}, ${event.city}`}
                    minPrice={event.minPrice}
                    posterUrl={event.posterUrl}
                    posterSrcset={event.posterSrcset}
                  />
                ))}
              </ul>
              {!stale && results.items.length < results.total && (
                <button type="button" className="button button--outline results__more" disabled={loadingMore} onClick={loadMore}>
                  {loadingMore ? 'Yükleniyor…' : `Daha fazla göster (${results.total - results.items.length})`}
                </button>
              )}
            </>
          )
        )}
      </div>
    </>
  )
}
