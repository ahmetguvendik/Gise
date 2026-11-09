import type { AccountActivity } from '../types'
import { request } from './http'

export const identityApi = {
  // Son girişler (hatalı denemeler dahil) ve parola, e-posta, iki aşamalı doğrulama değişiklikleri; yeniden eskiye.
  activity: (token: string) => request<AccountActivity[]>('/api/account/activity', { token }),
}
