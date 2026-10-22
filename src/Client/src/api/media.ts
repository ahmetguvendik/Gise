import type { CommitUpload, MediaPolicy, PresignedUpload } from '../types'
import { ApiError, request } from './http'

/**
 * Afiş yükleme üç adımda ilerler ve dosya hiçbir zaman API'den geçmez:
 * izin al → doğrudan depoya (MinIO) yükle → doğrulat.
 */
export const mediaApi = {
  policy: () => request<MediaPolicy>('/api/media/policy'),

  /** Dosyayı depoya yükleyip kalıcı anahtarını döndürür. */
  upload: async (file: File, token: string): Promise<CommitUpload> => {
    const ticket = await request<PresignedUpload>('/api/media/upload-url', {
      method: 'POST',
      body: { contentType: file.type, sizeBytes: file.size },
      token,
    })

    // İmzalı adrese doğrudan PUT. Content-Type imzaya dahil: birebir aynısı gönderilmezse depo reddeder.
    let response: Response
    try {
      response = await fetch(ticket.uploadUrl, {
        method: 'PUT',
        headers: { 'Content-Type': ticket.contentType },
        body: file,
      })
    } catch {
      throw new ApiError(0, 'Dosya depoya yüklenemedi. Bağlantınızı kontrol edip tekrar deneyin.')
    }
    if (!response.ok) {
      throw new ApiError(response.status, `Dosya depoya yüklenemedi (HTTP ${response.status}).`)
    }

    return request<CommitUpload>('/api/media/commit', { method: 'POST', body: { key: ticket.key }, token })
  },
}
