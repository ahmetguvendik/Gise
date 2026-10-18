import type { VenueSectionInput } from '../types'

interface Props {
  sections: VenueSectionInput[]
  onChange: (sections: VenueSectionInput[]) => void
  disabled?: boolean
}

// Yeni sıranın adı: önceki sıra sayıysa bir fazlası, değilse sıra numarası.
const nextRowName = (rows: VenueSectionInput['rows']) => {
  const last = Number(rows.at(-1)?.row)
  return String(Number.isInteger(last) ? last + 1 : rows.length + 1)
}

export function LayoutEditor({ sections, onChange, disabled = false }: Props) {
  const updateSection = (index: number, next: VenueSectionInput) =>
    onChange(sections.map((section, i) => (i === index ? next : section)))

  return (
    <div className="layout-editor">
      {sections.map((section, sectionIndex) => (
        <fieldset key={sectionIndex} className="section-editor" disabled={disabled}>
          <legend className="visually-hidden">{section.name || `${sectionIndex + 1}. bölüm`}</legend>
          <div className="section-editor__head">
            <label>
              Bölüm adı
              <input
                value={section.name}
                placeholder="ör. A Blok, Balkon"
                onChange={(e) => updateSection(sectionIndex, { ...section, name: e.target.value })}
                required
              />
            </label>
            <button
              type="button"
              className="button button--ghost"
              onClick={() => onChange(sections.filter((_, i) => i !== sectionIndex))}
              disabled={sections.length === 1}
            >
              Bölümü kaldır
            </button>
          </div>

          <div className="row-editor">
            {section.rows.map((row, rowIndex) => (
              <div key={rowIndex} className="row-editor__row">
                <label>
                  Sıra
                  <input
                    value={row.row}
                    onChange={(e) =>
                      updateSection(sectionIndex, {
                        ...section,
                        rows: section.rows.map((r, i) => (i === rowIndex ? { ...r, row: e.target.value } : r)),
                      })
                    }
                    required
                  />
                </label>
                <label>
                  Koltuk
                  <input
                    type="number"
                    min="1"
                    max="100"
                    value={row.seatCount}
                    onChange={(e) =>
                      updateSection(sectionIndex, {
                        ...section,
                        rows: section.rows.map((r, i) => (i === rowIndex ? { ...r, seatCount: Number(e.target.value) } : r)),
                      })
                    }
                    required
                  />
                </label>
                <button
                  type="button"
                  className="button button--ghost"
                  aria-label={`${row.row} sırasını kaldır`}
                  onClick={() => updateSection(sectionIndex, { ...section, rows: section.rows.filter((_, i) => i !== rowIndex) })}
                  disabled={section.rows.length === 1}
                >
                  Kaldır
                </button>
              </div>
            ))}
          </div>

          <button
            type="button"
            className="button button--outline"
            onClick={() =>
              updateSection(sectionIndex, {
                ...section,
                rows: [...section.rows, { row: nextRowName(section.rows), seatCount: section.rows.at(-1)?.seatCount ?? 10 }],
              })
            }
          >
            + Sıra ekle
          </button>
        </fieldset>
      ))}

      {!disabled && (
        <button
          type="button"
          className="button button--contrast"
          onClick={() => onChange([...sections, { name: '', rows: [{ row: '1', seatCount: 10 }] }])}
        >
          + Bölüm ekle
        </button>
      )}
    </div>
  )
}
