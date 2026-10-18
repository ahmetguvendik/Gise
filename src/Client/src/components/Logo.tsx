// Koçan: kenarları oyuk bilet koçanı ve kesik çizgi. Renkleri CSS değişkenlerinden (index.css: .ticket-mark__*).
export function TicketMark({ width = 42 }: { width?: number }) {
  return (
    <svg width={width} height={(width * 40) / 56} viewBox="0 0 56 40" aria-hidden="true">
      <path
        className="ticket-mark__body"
        d="M4 0H52A4 4 0 0 1 56 4V14A6 6 0 0 0 56 26V36A4 4 0 0 1 52 40H4A4 4 0 0 1 0 36V26A6 6 0 0 0 0 14V4A4 4 0 0 1 4 0Z"
      />
      <path className="ticket-mark__cut" d="M19 7V33" strokeWidth="3" strokeDasharray="3 3.5" strokeLinecap="round" />
    </svg>
  )
}

export function Logo({ small = false }: { small?: boolean }) {
  return (
    <span className={small ? 'brand brand--sm' : 'brand'}>
      <TicketMark width={small ? 34 : 42} />
      <span className="brand__name">Gişe</span>
    </span>
  )
}
