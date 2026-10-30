import { useEffect, useId, useState } from 'react'
import type { KeyboardEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { searchApi } from '../api/search'
import { formatDate } from '../lib/format'
import type { Suggestions } from '../types'

// Öneri, yazmaya kısa bir ara verilince istenir; her tuşta değil. Uzunluk sınırları API'ninkiyle aynı.
const SUGGEST_DELAY_MS = 200
const MIN_SUGGEST_LENGTH = 2
const MAX_SUGGEST_LENGTH = 50
const MAX_QUERY_LENGTH = 100

type Option =
  | { kind: 'event'; key: string; id: string; title: string; detail: string }
  | { kind: 'venue'; key: string; name: string }
  | { kind: 'city'; key: string; name: string }

const GROUPS: { kind: Option['kind']; label: string }[] = [
  { kind: 'event', label: 'Etkinlikler' },
  { kind: 'venue', label: 'Mekânlar' },
  { kind: 'city', label: 'Şehirler' },
]

function toOptions(suggestions: Suggestions): Option[] {
  return [
    ...suggestions.events.map((event): Option => ({
      kind: 'event',
      key: `event-${event.id}`,
      id: event.id,
      title: event.title,
      detail: `${event.venueName}, ${event.city} · ${formatDate(event.startsAt)}`,
    })),
    ...suggestions.venues.map((name): Option => ({ kind: 'venue', key: `venue-${name}`, name })),
    ...suggestions.cities.map((name): Option => ({ kind: 'city', key: `city-${name}`, name })),
  ]
}

interface Props {
  /** URL'deki arama; dışarıdan değişince (geri tuşu, filtreleri temizleme) kutu da değişir. */
  value: string
  onSearch: (text: string) => void
  onCity: (city: string) => void
}

// Etkinlik önerisi etkinliğin sayfasını açar, mekân önerisi o adla arar, şehir önerisi şehir filtresi olur.
export function SearchBox({ value, onSearch, onCity }: Props) {
  const navigate = useNavigate()
  const listId = useId()
  const [text, setText] = useState(value)
  const [lastValue, setLastValue] = useState(value)
  const [suggestions, setSuggestions] = useState<Suggestions | null>(null)
  const [open, setOpen] = useState(false)
  const [active, setActive] = useState(-1)

  if (value !== lastValue) {
    setLastValue(value)
    setText(value)
  }

  const query = text.trim()
  const canSuggest = query.length >= MIN_SUGGEST_LENGTH && query.length <= MAX_SUGGEST_LENGTH

  useEffect(() => {
    if (!canSuggest) return
    // Geç gelen eski bir yanıt, yenisinin üzerine yazmasın.
    let ignore = false
    const timer = setTimeout(() => {
      searchApi.suggest(query).then(
        (data) => {
          if (ignore) return
          setSuggestions(data)
          setActive(-1)
        },
        // Öneri gelmezse arama yine çalışır; kutu sessizce önerisiz kalır.
        () => {
          if (!ignore) setSuggestions(null)
        },
      )
    }, SUGGEST_DELAY_MS)
    return () => {
      ignore = true
      clearTimeout(timer)
    }
  }, [query, canSuggest])

  const options = canSuggest && suggestions ? toOptions(suggestions) : []
  const expanded = open && options.length > 0

  const close = () => {
    setOpen(false)
    setActive(-1)
  }

  const choose = (option: Option) => {
    close()
    if (option.kind === 'event') {
      navigate(`/etkinlik/${option.id}`)
    } else if (option.kind === 'venue') {
      setText(option.name)
      onSearch(option.name)
    } else {
      // Yazılan şehrin adıydı; metin aramadan çıkar, şehir filtresi olur.
      setText('')
      onCity(option.name)
    }
  }

  const onKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'ArrowDown' && options.length > 0) {
      event.preventDefault()
      setOpen(true)
      setActive((current) => (current + 1) % options.length)
    } else if (event.key === 'ArrowUp' && options.length > 0) {
      event.preventDefault()
      setOpen(true)
      setActive((current) => (current <= 0 ? options.length - 1 : current - 1))
    } else if (event.key === 'Enter' && expanded && active >= 0) {
      event.preventDefault()
      choose(options[active])
    } else if (event.key === 'Escape' && expanded) {
      event.preventDefault()
      close()
    }
  }

  return (
    <div className="search">
      <form
        className="search__form"
        role="search"
        onSubmit={(event) => {
          event.preventDefault()
          close()
          onSearch(query)
        }}
      >
        <svg className="search__icon" width="22" height="22" viewBox="0 0 24 24" aria-hidden="true">
          <circle cx="11" cy="11" r="7" fill="none" stroke="currentColor" strokeWidth="2.4" />
          <path d="m16.5 16.5 4.5 4.5" stroke="currentColor" strokeWidth="2.4" strokeLinecap="round" />
        </svg>
        <input
          className="search__input"
          type="search"
          role="combobox"
          aria-label="Etkinlik, mekân ya da şehir ara"
          aria-autocomplete="list"
          aria-expanded={expanded}
          aria-controls={listId}
          aria-activedescendant={expanded && active >= 0 ? `${listId}-${active}` : undefined}
          placeholder="Etkinlik, mekân ya da şehir"
          autoComplete="off"
          enterKeyHint="search"
          maxLength={MAX_QUERY_LENGTH}
          value={text}
          onChange={(event) => {
            setText(event.target.value)
            setOpen(true)
            setActive(-1)
          }}
          onFocus={() => setOpen(true)}
          onBlur={close}
          onKeyDown={onKeyDown}
        />
        {text && (
          <button
            className="search__clear"
            type="button"
            aria-label="Aramayı temizle"
            onClick={() => {
              setText('')
              close()
              if (value) onSearch('')
            }}
          >
            ×
          </button>
        )}
        <button className="button" type="submit">
          Ara
        </button>
      </form>

      <div id={listId} className="suggest" role="listbox" aria-label="Öneriler" hidden={!expanded}>
        {GROUPS.map(({ kind, label }) => {
          const group = options.map((option, index) => ({ option, index })).filter(({ option }) => option.kind === kind)
          if (group.length === 0) return null
          return (
            <div key={kind} className="suggest__group" role="group" aria-labelledby={`${listId}-${kind}`}>
              <div id={`${listId}-${kind}`} className="suggest__label">
                {label}
              </div>
              {group.map(({ option, index }) => (
                <div
                  key={option.key}
                  id={`${listId}-${index}`}
                  className={`suggest__option${index === active ? ' is-active' : ''}`}
                  role="option"
                  aria-selected={index === active}
                  // Tıklama kutunun odağını almasın; yoksa öneri listesi tıklamadan önce kapanır.
                  onMouseDown={(event) => event.preventDefault()}
                  onMouseEnter={() => setActive(index)}
                  onClick={() => choose(option)}
                >
                  <span className="suggest__title">{option.kind === 'event' ? option.title : option.name}</span>
                  {option.kind === 'event' && <span className="suggest__detail">{option.detail}</span>}
                </div>
              ))}
            </div>
          )
        })}
      </div>
    </div>
  )
}
