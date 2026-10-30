// Ana sayfadaki tarih filtresi. Günler tarayıcının saatine göre hesaplanır; API'ye UTC gider.
export type DatePreset = 'bugun' | 'hafta-sonu' | 'bu-ay' | 'aralik'

export const DATE_PRESETS: { value: DatePreset; label: string }[] = [
  { value: 'bugun', label: 'Bugün' },
  { value: 'hafta-sonu', label: 'Bu hafta sonu' },
  { value: 'bu-ay', label: 'Bu ay' },
  { value: 'aralik', label: 'Tarih seç' },
]

const startOfDay = (date: Date) => new Date(date.getFullYear(), date.getMonth(), date.getDate())
const endOfDay = (date: Date) => new Date(date.getFullYear(), date.getMonth(), date.getDate(), 23, 59, 59, 999)

/** Tarih alanı değeri ("2026-10-30") yerel gün olarak; geçersizse boş. */
const parseDay = (value: string) => {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value)
  return match ? new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3])) : undefined
}

/** Tarih alanı için bugünün değeri: "2026-10-30". */
export const todayValue = (now = new Date()) => {
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`
}

/** Başlangıç verilmezse API şu andan başlatır: bugünün geçmiş saatleri listelenmez. */
export function dateRange(preset: string, start: string, end: string, now = new Date()): { from?: Date; to?: Date } {
  switch (preset) {
    case 'bugun':
      return { to: endOfDay(now) }
    case 'hafta-sonu': {
      // getDay: 0 pazar, 6 cumartesi. Hafta sonundaysak şu andan pazar akşamına.
      const day = now.getDay()
      const saturday = new Date(now.getFullYear(), now.getMonth(), now.getDate() + (day === 0 ? -1 : 6 - day))
      const sunday = new Date(saturday.getFullYear(), saturday.getMonth(), saturday.getDate() + 1)
      return { from: day === 0 || day === 6 ? undefined : startOfDay(saturday), to: endOfDay(sunday) }
    }
    case 'bu-ay':
      return { to: new Date(now.getFullYear(), now.getMonth() + 1, 0, 23, 59, 59, 999) }
    case 'aralik': {
      const from = parseDay(start)
      const to = parseDay(end)
      return { from, to: to && endOfDay(to) }
    }
    default:
      return {}
  }
}
