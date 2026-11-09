interface Props {
  page: number
  pageSize: number
  totalCount: number
  disabled?: boolean
  onPageChange: (page: number) => void
}

// Sayfalı listelerin altı (Telgraf'taki Pagination): "11–20 / 45", önceki/sonraki ve "Sayfa 2 / 5". Tek sayfaysa görünmez.
export function Pagination({ page, pageSize, totalCount, disabled = false, onPageChange }: Props) {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))
  if (totalPages <= 1) return null

  const from = totalCount === 0 ? 0 : (page - 1) * pageSize + 1
  const to = Math.min(page * pageSize, totalCount)

  return (
    <nav className="pagination" aria-label="Sayfalar">
      <span className="pagination__info">
        {from}–{to} / {totalCount}
      </span>
      <div className="pagination__controls">
        <button type="button" className="button button--outline" disabled={disabled || page <= 1} onClick={() => onPageChange(page - 1)}>
          ‹ Önceki
        </button>
        <span className="pagination__page" aria-current="page">
          Sayfa {page} / {totalPages}
        </span>
        <button
          type="button"
          className="button button--outline"
          disabled={disabled || page >= totalPages}
          onClick={() => onPageChange(page + 1)}
        >
          Sonraki ›
        </button>
      </div>
    </nav>
  )
}
