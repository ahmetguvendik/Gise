import type { CreateEventInput, EventSummary } from '../types'
import { request } from './http'

export const eventsApi = {
  list: () => request<EventSummary[]>('/api/events'),

  create: (input: CreateEventInput, token: string) =>
    request<EventSummary>('/api/events', { method: 'POST', body: input, token }),
}
