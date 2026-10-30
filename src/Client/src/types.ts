/** PendingReview: organizatör onaya gönderdi, admin yayınlar ya da geri gönderir. */
export type EventStatus = 'Draft' | 'PendingReview' | 'Published' | 'Cancelled'

/** Arama sonucundaki etkinlik: yalnızca yayındaki ve yaklaşan etkinlikler. */
export interface SearchEventItem {
  id: string
  title: string
  startsAt: string
  venueName: string
  city: string
  minPrice: number
  /** Afiş yoksa boş; o zaman renkli yer tutucu çizilir. */
  posterUrl: string | null
  posterSrcset: string | null
  /** Bütün koltuklar satılmış (en fazla 1 dakika önceki durum). */
  soldOut: boolean
  /** Az kaldıysa (20 ve altı) satılmamış koltuk sayısı; değilse boş. */
  remainingSeats: number | null
}

export interface CityFacet {
  name: string
  count: number
}

export interface SearchEventsResult {
  items: SearchEventItem[]
  total: number
  page: number
  pageSize: number
  /** Aramaya ve tarihe uyan etkinliklerin şehirleri; seçili şehirden bağımsız. */
  cities: CityFacet[]
}

export interface SuggestedEvent {
  id: string
  title: string
  startsAt: string
  venueName: string
  city: string
}

export interface Suggestions {
  events: SuggestedEvent[]
  venues: string[]
  cities: string[]
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

/** Koltuk haritası (Reservation): hangi koltuk boş, bölüm fiyatları. */
export interface SeatMap {
  eventId: string
  startsAt: string
  /** İptal edilmemiş ve başlamamışsa satışta. */
  isOnSale: boolean
  /** Bütün koltuklar satılmış; sepette tutulanlar satılmamış sayılır. */
  soldOut: boolean
  sections: SeatMapSection[]
}

export interface SeatMapSection {
  sectionId: string
  sectionName: string
  price: number
  availableCount: number
  seats: SeatMapSeat[]
}

export interface SeatMapSeat {
  seatId: string
  row: string
  number: number
  /** Tutulmuş ya da satılmış koltuk ayrımı dışarıya verilmez. */
  isAvailable: boolean
}

/**
 * Pending: koltuklar tutuluyor; AwaitingPayment: koltuklar ayrıldı, ödeme bekleniyor (son deneme reddedildiyse sebep
 * failureReason'da); PaymentProcessing: ödeme, koltuk satışı ve biletler sürüyor; Completed: ödendi, biletler hazır;
 * Cancelled ve Expired kapanmış siparişlerdir (sebep failureReason'da); Refunded: etkinlik iptal edildi, para iade edildi.
 */
export type OrderStatus = 'Pending' | 'AwaitingPayment' | 'PaymentProcessing' | 'Completed' | 'Cancelled' | 'Expired' | 'Refunded'

export interface OrderItem {
  seatId: string
  sectionName: string
  row: string
  number: number
  price: number
}

/** Etkinlik bilgisi sipariş açılırken Catalog'dan kopyalanır. */
interface OrderEvent {
  eventId: string
  eventTitle: string
  eventStartsAt: string
  venueName: string
  city: string
}

export interface OrderDetail extends OrderEvent {
  id: string
  status: OrderStatus
  totalAmount: number
  /** Ödeme bu zamana kadar gelmezse sipariş düşer. */
  expiresAt: string
  failureReason: string | null
  /** Son ödeme denemesi: "Öde"nin sonucu bununla tanınır. */
  lastPaymentId: string | null
  createdAt: string
  items: OrderItem[]
}

export interface OrderSummary extends OrderEvent {
  id: string
  status: OrderStatus
  totalAmount: number
  seatCount: number
  expiresAt: string
  createdAt: string
}

export interface CreatedOrder {
  orderId: string
  status: OrderStatus
}

/** Valid: girişte okutulabilir; Used: okutuldu; Cancelled: geçersiz (ör. etkinlik iptal edildi). */
/** Transferred: gönderenin listesinde, başkasına aktarılan bilet. */
export type TicketStatus = 'Valid' | 'Used' | 'Cancelled' | 'Transferred'

/** Etkinlik ve koltuk bilgisi bilet çıkarken siparişten kopyalanır. */
export interface TicketItem {
  id: string
  orderId: string
  eventId: string
  eventTitle: string
  eventStartsAt: string
  venueName: string
  city: string
  sectionName: string
  row: string
  number: number
  price: number
  /** Kapıda okutulan kod, okunaklı biçimde: "ABCD-EFGH-JKMN-PQRS". Aktarılan bilette boş: kod artık alıcının. */
  code: string
  status: TicketStatus
  checkedInAt: string | null
  /** Aktarımın kabul zamanı: aktarımla gelen bilette geldiği, Transferred'da gittiği an. */
  transferredAt: string | null
  /** Transferred'da alıcının e-postası. */
  transferredTo: string | null
  /** Geçerli, aktarımla gelmemiş, bekleyen aktarımı yok ve süre dolmamış. */
  canTransfer: boolean
  /** Aktarım için son an (etkinlikten 2 saat önce). */
  transferDeadline: string
  /** Alıcının kabulünü bekleyen aktarım. */
  pendingTransfer: PendingTransfer | null
}

export interface PendingTransfer {
  id: string
  toEmail: string
  createdAt: string
}

/** Giriş yapanın e-postasına gönderilmiş, kabul bekleyen bilet. */
export interface IncomingTransfer {
  transferId: string
  ticketId: string
  fromName: string
  eventId: string
  eventTitle: string
  eventStartsAt: string
  venueName: string
  city: string
  sectionName: string
  row: string
  number: number
  /** Bu andan sonra kabul edilemez. */
  expiresAt: string
}

export interface OfferedTransfer {
  transferId: string
  toEmail: string
  expiresAt: string
}

/** Kabul edilince bilete yeni kod verilir. */
export interface AcceptedTransfer {
  ticketId: string
  code: string
}

/** PDF API'den geçmez; kısa süreli imzalı adres. */
export interface TicketDownload {
  url: string
  expiresInSeconds: number
}

/** Kapı sayfası: okutması şu an açık etkinlik. */
export interface GateEvent {
  eventId: string
  eventTitle: string
  eventStartsAt: string
  venueName: string
  city: string
  opensAt: string
  closesAt: string
  ticketCount: number
  checkedInCount: number
}

/** Kapıda geçerli bilet okutulunca görevliye gösterilir. */
export interface CheckInResult {
  ticketId: string
  eventTitle: string
  eventStartsAt: string
  sectionName: string
  row: string
  number: number
  checkedInAt: string
}

/** Kart yalnızca Order'da doğrulanır; Payment'a son dört hanesi gider. */
export interface PaymentRequest {
  /** Boşluksuz 16 hane. */
  cardNumber: string
  expiryMonth: number
  /** Dört haneli yıl. */
  expiryYear: number
  cvc: string
}

export interface PaymentAccepted {
  paymentId: string
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
  errorCode?: string
}
