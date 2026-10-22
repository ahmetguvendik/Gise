/** PendingReview: organizatör onaya gönderdi, admin yayınlar ya da geri gönderir. */
export type EventStatus = 'Draft' | 'PendingReview' | 'Published' | 'Cancelled'

export interface EventSummary {
  id: string
  title: string
  venueId: string
  startsAt: string
  /** Afiş yoksa boş; o zaman renkli yer tutucu çizilir. */
  posterUrl: string | null
  posterSrcset: string | null
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
  /** Afiş yoksa boş; o zaman renkli yer tutucu çizilir. */
  posterUrl: string | null
  posterSrcset: string | null
  organizerId: string
  createdAt: string
  /** Admin taslağa geri gönderdiyse sebebi; yalnızca sahibi ve admin görür. */
  returnReason: string | null
  /** İptal edildiyse dolu; sayfada sebebiyle gösterilir. */
  cancelledAt: string | null
  cancellationReason: string | null
  prices: EventPrice[]
}

/** Organizatörün kendi etkinlikleri: taslaklar dahil. */
export interface MyEvent {
  id: string
  title: string
  status: EventStatus
  startsAt: string
  /** Afiş yoksa boş; o zaman renkli yer tutucu çizilir. */
  posterUrl: string | null
  posterSrcset: string | null
  venueId: string
  venueName: string
  city: string
  minPrice: number | null
}

/** Admin'in onay bekleyenler listesi; alanlar Etkinliklerim'dekiyle aynı. */
export type PendingEvent = MyEvent

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
  sectionCount: number
  seatCount: number
  /** Etkinliği olan mekânın bölüm düzeni değişmez, mekân silinemez. */
  hasEvents: boolean
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

export interface VenueDetail {
  id: string
  name: string
  city: string
  hasEvents: boolean
  sections: VenueSection[]
}

/** Mekân oluşturma ve düzen güncellemede gönderilen bölüm; koltuklar sıradan üretilir. */
export interface VenueSectionInput {
  name: string
  rows: VenueRow[]
}

/** Yükleme izni: dosya bu adrese doğrudan PUT edilir. */
export interface PresignedUpload {
  uploadUrl: string
  key: string
  contentType: string
  expiresInSeconds: number
}

/** Doğrulanmış yükleme: `key` etkinliğin afiş alanına yazılır, `url` önizleme içindir. */
export interface CommitUpload {
  key: string
  url: string | null
}

export interface MediaPolicy {
  maxSizeBytes: number
  allowedContentTypes: string[]
}

/** API'nin hata gövdesi (RFC 7807 ProblemDetails). */
export interface ProblemDetails {
  title?: string
  detail?: string
  status?: number
  errors?: Record<string, string[]>
}
