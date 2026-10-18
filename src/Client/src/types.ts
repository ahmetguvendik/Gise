export interface EventSummary {
  id: string
  title: string
  venueId: string
  startsAt: string
  /** Yayındaki etkinlikte her bölüm fiyatlıdır; yine de API boş dönebilir. */
  minPrice: number | null
}

export interface CreateEventInput {
  title: string
  venueId: string
  startsAt: string
}

export interface CreatedEvent {
  id: string
  title: string
  status: string
}

export interface VenueSummary {
  id: string
  name: string
  city: string
}

/** API'nin hata gövdesi (RFC 7807 ProblemDetails). */
export interface ProblemDetails {
  title?: string
  status?: number
  errors?: Record<string, string[]>
}
