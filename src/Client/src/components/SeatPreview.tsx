import type { VenueSectionInput } from '../types'

// Düzenin koltuk koltuk çizimi: her sıra bir satır, her koltuk bir nokta. İleride koltuk seçme haritasının temeli.
export function SeatPreview({ sections }: { sections: VenueSectionInput[] }) {
  const total = sections.reduce((sum, s) => sum + s.rows.reduce((rowSum, r) => rowSum + (r.seatCount || 0), 0), 0)

  return (
    <figure className="seat-preview" aria-label={`Koltuk düzeni önizlemesi, toplam ${total} koltuk`}>
      <div className="seat-preview__stage">SAHNE</div>
      {sections.map((section, sectionIndex) => (
        <div key={sectionIndex} className="seat-preview__section">
          <p className="seat-preview__name">{section.name || `${sectionIndex + 1}. bölüm`}</p>
          {section.rows.map((row, rowIndex) => (
            <div key={rowIndex} className="seat-preview__row">
              <span className="seat-preview__label">{row.row}</span>
              <span className="seat-preview__seats" aria-hidden="true">
                {Array.from({ length: Math.max(0, Math.min(row.seatCount || 0, 100)) }, (_, i) => (
                  <span key={i} className="seat-dot" />
                ))}
              </span>
            </div>
          ))}
        </div>
      ))}
      <figcaption className="muted">Toplam {total} koltuk</figcaption>
    </figure>
  )
}
