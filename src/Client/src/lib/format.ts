const longDateFormatter = new Intl.DateTimeFormat('tr-TR', { day: 'numeric', month: 'long', year: 'numeric', weekday: 'long' })
const dateTimeFormatter = new Intl.DateTimeFormat('tr-TR', { dateStyle: 'long', timeStyle: 'short' })
const timeFormatter = new Intl.DateTimeFormat('tr-TR', { hour: '2-digit', minute: '2-digit' })
const weekdayTimeFormatter = new Intl.DateTimeFormat('tr-TR', { weekday: 'long', hour: '2-digit', minute: '2-digit' })
const dayFormatter = new Intl.DateTimeFormat('tr-TR', { day: 'numeric' })
const monthFormatter = new Intl.DateTimeFormat('tr-TR', { month: 'short' })

/** "20 Ekim 2026 19:30" */
export const formatDate = (iso: string) => dateTimeFormatter.format(new Date(iso))
/** "20 Ekim 2026 Salı" */
export const formatLongDate = (iso: string) => longDateFormatter.format(new Date(iso))
/** "19:30" */
export const formatTime = (iso: string) => timeFormatter.format(new Date(iso))
/** "Salı 19:30" */
export const formatWeekdayTime = (iso: string) => weekdayTimeFormatter.format(new Date(iso))
/** Tarih kutusu için: "20" ve "EKİ" (Türkçe büyük harf: i → İ). */
export const formatDay = (iso: string) => dayFormatter.format(new Date(iso))
export const formatMonthShort = (iso: string) => monthFormatter.format(new Date(iso)).replace('.', '').toLocaleUpperCase('tr-TR')

/** Tam sayıysa kuruşsuz: "₺500", değilse "₺750,50". */
export const formatPrice = (price: number) =>
  new Intl.NumberFormat('tr-TR', {
    style: 'currency',
    currency: 'TRY',
    minimumFractionDigits: Number.isInteger(price) ? 0 : 2,
    maximumFractionDigits: 2,
  }).format(price)
