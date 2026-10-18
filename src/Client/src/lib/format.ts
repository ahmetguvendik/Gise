const dateFormatter = new Intl.DateTimeFormat('tr-TR', { dateStyle: 'long', timeStyle: 'short' })
const priceFormatter = new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY', maximumFractionDigits: 2 })

export const formatDate = (iso: string) => dateFormatter.format(new Date(iso))
export const formatPrice = (price: number) => priceFormatter.format(price)
