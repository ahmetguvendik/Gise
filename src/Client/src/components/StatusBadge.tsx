import type { EventStatus } from '../types'

const LABELS: Record<EventStatus, string> = {
  Draft: 'Taslak',
  Published: 'Yayında',
  Cancelled: 'İptal',
}

export function StatusBadge({ status }: { status: EventStatus }) {
  return <span className={`badge badge--${status.toLowerCase()}`}>{LABELS[status]}</span>
}
