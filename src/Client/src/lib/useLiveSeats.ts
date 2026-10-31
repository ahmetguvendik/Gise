import { useEffect, useRef, useState } from 'react'
import { HttpTransportType, HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import type { SeatsChanged } from '../types'

// Koltuk haritasının anlık güncellenmesi (SeatMap, bkz. tasarım 3.11). Bağlantı gateway üzerinden yalnızca WebSocket'le
// kurulur; kopunca kendisi yeniden bağlanır. İzleme başlayınca ve yeniden bağlanınca onResync çağrılır: arada kaçan
// değişiklikler için harita baştan yüklenir. Bağlantı hiç kurulamazsa harita yalnızca anlık olmaz, sayfa çalışmaya devam
// eder. Dönen değer: güncellemeler şu an geliyor mu.
export function useLiveSeats(eventId: string, onChange: (change: SeatsChanged) => void, onResync: () => void) {
  const [live, setLive] = useState(false)
  // Bağlantı her render'da yeniden kurulmasın; geri çağrıların en güncel hâli burada tutulur.
  const handlers = useRef({ onChange, onResync })
  useEffect(() => {
    handlers.current = { onChange, onResync }
  })

  useEffect(() => {
    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/seatmap', { transport: HttpTransportType.WebSockets, skipNegotiation: true })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    const watch = async () => {
      await connection.invoke('Watch', eventId)
      setLive(true)
      handlers.current.onResync()
    }

    connection.on('SeatsChanged', (change: SeatsChanged) => {
      if (change.eventId === eventId) handlers.current.onChange(change)
    })
    connection.onreconnecting(() => setLive(false))
    connection.onreconnected(() => void watch().catch(() => setLive(false)))
    connection.onclose(() => setLive(false))

    void connection
      .start()
      .then(watch)
      .catch(() => setLive(false))

    return () => {
      void connection.stop()
    }
  }, [eventId])

  return live
}
