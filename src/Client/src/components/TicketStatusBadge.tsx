import type { TicketStatus } from '../types'

const LABELS: Record<TicketStatus, string> = {
  Valid: 'Geçerli',
  Used: 'Kullanıldı',
  Cancelled: 'İptal',
  Transferred: 'Aktarıldı',
}

export function TicketStatusBadge({ status }: { status: TicketStatus }) {
  return <span className={`badge badge--ticket-${status.toLowerCase()}`}>{LABELS[status]}</span>
}
