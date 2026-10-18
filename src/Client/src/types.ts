export type EventStatus = 'Draft' | 'Published' | 'Cancelled'

export interface EventSummary {
  id: string
  title: string
  venueId: string
  startsAt: string
  /** Yayındaki etkinlikte her bölüm fiyatlıdır; yine de API boş dönebilir. */
  minPrice: number | null
}

export interface EventPrice {
  sectionId: string
  sectionName: string
  price: number
}

export interface EventDetail {
  id: string
  title: string
  venueId: string
  status: EventStatus
  startsAt: string
  organizerId: string
  createdAt: string
  prices: EventPrice[]
}

/** Organizatörün kendi etkinlikleri: taslaklar dahil. */
export interface MyEvent {
  id: string
  title: string
  status: EventStatus
  startsAt: string
  venueId: string
  venueName: string
  city: string
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
  status: EventStatus
}

export interface PriceInput {
  sectionId: string
  price: number
}

export interface VenueSummary {
  id: string
  name: string
  city: string
}

export interface VenueRow {
  row: string
  seatCount: number
}

export interface VenueSection {
  id: string
  name: string
  rows: VenueRow[]
}

export interface VenueDetail extends VenueSummary {
  sections: VenueSection[]
}

/** API'nin hata gövdesi (RFC 7807 ProblemDetails). */
export interface ProblemDetails {
  title?: string
  detail?: string
  status?: number
  errors?: Record<string, string[]>
}
