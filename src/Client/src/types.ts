export interface EventSummary {
  id: string
  title: string
  venue: string
  startsAt: string
}

export interface CreateEventInput {
  title: string
  venue: string
  startsAt: string
}

/** API'nin hata gövdesi (RFC 7807 ProblemDetails). */
export interface ProblemDetails {
  title?: string
  status?: number
  errors?: Record<string, string[]>
}
