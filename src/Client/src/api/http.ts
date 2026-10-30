import type { ProblemDetails } from '../types'

export class ApiError extends Error {
  readonly status: number
  readonly fieldErrors: Record<string, string[]>
  /** Ayrıntı, ör. "Fiyatı olmayan bölümler: Balkon." */
  readonly detail?: string
  /** Merkezi hata kodu, ör. "TICKET_WRONG_EVENT"; aynı HTTP durumundaki hataları ayırmak için. */
  readonly code?: string

  constructor(status: number, message: string, fieldErrors: Record<string, string[]> = {}, detail?: string, code?: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.fieldErrors = fieldErrors
    this.detail = detail
    this.code = code
  }
}

export const toApiError = (err: unknown) =>
  err instanceof ApiError ? err : new ApiError(0, 'Beklenmeyen bir hata oluştu.')

interface RequestOptions {
  method?: string
  body?: unknown
  token?: string
}

export async function request<T>(path: string, { method = 'GET', body, token }: RequestOptions = {}): Promise<T> {
  let response: Response
  try {
    response = await fetch(path, {
      method,
      headers: {
        ...(body !== undefined ? { 'Content-Type': 'application/json' } : {}),
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      body: body !== undefined ? JSON.stringify(body) : undefined,
    })
  } catch {
    throw new ApiError(0, 'Sunucuya ulaşılamıyor. Biraz sonra tekrar deneyin.')
  }

  if (!response.ok) {
    const problem: ProblemDetails = await response.json().catch(() => ({}))
    throw new ApiError(
      response.status,
      problem.title ?? `İstek başarısız oldu (HTTP ${response.status}).`,
      problem.errors,
      problem.detail,
      problem.errorCode,
    )
  }

  return (response.status === 204 ? undefined : await response.json()) as T
}
