import type { CreatedEvent, CreateEventInput, EventSummary } from '../types'
import { request } from './http'

export const eventsApi = {
  list: () => request<EventSummary[]>('/api/events'),

  create: (input: CreateEventInput, token: string) =>
    request<CreatedEvent>('/api/events', { method: 'POST', body: input, token }),
}
