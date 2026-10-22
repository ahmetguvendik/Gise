import type { CSSProperties } from 'react'
import { formatDay, formatMonthShort } from '../lib/format'

// Afiş yüklenmemişse etkinliğin adından üretilen renkli blok. Renk ID'den seçilir: her yüklemede aynı.
const TONES = ['accent', 'cobalt', 'sun', 'ink'] as const

function toneOf(id: string) {
  let hash = 0
  for (const char of id) hash = (hash * 31 + char.charCodeAt(0)) | 0
  return TONES[Math.abs(hash) % TONES.length]
}

// Afiş yüksekliği sabit: başlık uzadıkça yazı küçülür, son satır kesilmesin.
function titleScale(title: string) {
  const words = title.trim().split(/\s+/)
  const longest = Math.max(...words.map((word) => word.length))
  if (words.length >= 5 || longest > 12) return 0.5
  if (words.length >= 3 || longest > 8) return 0.64
  return 1
}

interface Props {
  id: string
  title: string
  startsAt: string
  size?: 'md' | 'lg'
  imageUrl?: string | null
  imageSrcset?: string | null
}

// Yalnızca görsel: başlık ve tarih sayfada ayrıca yazıyor, ekran okuyucu ikinci kez okumasın.
export function Poster({ id, title, startsAt, size = 'md', imageUrl, imageSrcset }: Props) {
  const dateChip = (
    <span className="date-chip">
      <strong>{formatDay(startsAt)}</strong>
      <span>{formatMonthShort(startsAt)}</span>
    </span>
  )

  if (imageUrl) {
    return (
      <div className={`poster poster--image${size === 'lg' ? ' poster--lg' : ''}`} aria-hidden="true">
        {/* Tarayıcı gösterilen genişliğe uyan boyu (400/800/1200) seçer. */}
        <img
          className="poster__image"
          src={imageUrl}
          srcSet={imageSrcset ?? undefined}
          sizes={size === 'lg' ? '(min-width: 1024px) 640px, 100vw' : '(min-width: 768px) 400px, 100vw'}
          alt=""
          loading={size === 'lg' ? 'eager' : 'lazy'}
          decoding="async"
        />
        {dateChip}
      </div>
    )
  }

  return (
    <div
      className={`poster poster--${toneOf(id)}${size === 'lg' ? ' poster--lg' : ''}`}
      style={{ '--title-scale': titleScale(title) } as CSSProperties}
      aria-hidden="true"
    >
      {dateChip}
      <p className="poster__title">{title.toLocaleUpperCase('tr-TR')}</p>
    </div>
  )
}
