import type { OrderStatus } from '../types'

const LABELS: Record<OrderStatus, string> = {
  Pending: 'Koltuklar tutuluyor',
  AwaitingPayment: 'Ödeme bekleniyor',
  Completed: 'Tamamlandı',
  Cancelled: 'İptal',
  Expired: 'Süresi doldu',
}

export function OrderStatusBadge({ status }: { status: OrderStatus }) {
  return <span className={`badge badge--order-${status.toLowerCase()}`}>{LABELS[status]}</span>
}
