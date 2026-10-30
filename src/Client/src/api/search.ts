import type { SearchEventsResult, Suggestions } from '../types'
import { request } from './http'

export interface SearchCriteria {
  q?: string
  city?: string
  /** Verilmezse şu an. */
  from?: Date
  to?: Date
  /** 1'den başlar. */
  page: number
  pageSize: number
}

// Herkese açık; giriş gerekmez.
export const searchApi = {
  // Boş arama yaklaşan etkinlikleri tarihe göre, metinli arama en uygun olanı önce döndürür.
  events: ({ q, city, from, to, page, pageSize }: SearchCriteria) => {
    const query = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
    if (q) query.set('q', q)
    if (city) query.set('city', city)
    if (from) query.set('from', from.toISOString())
    if (to) query.set('to', to.toISOString())
    return request<SearchEventsResult>(`/api/search/events?${query}`)
  },

  // Yazarken öneri: etkinlikler, mekânlar ve şehirler.
  suggest: (q: string) => request<Suggestions>(`/api/search/suggest?${new URLSearchParams({ q })}`),
}
