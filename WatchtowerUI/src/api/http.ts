import { tokenStore, decodeJwt } from '../auth/tokenStore'
import type { ApiErrorBody, AuthTokens } from './types'

export class ApiError extends Error {
  readonly status: number
  readonly errors: string[]

  constructor(status: number, message: string, errors: string[] = []) {
    super(message)
    this.status = status
    this.errors = errors
  }
}

// Invoked when a refresh attempt fails while the user was signed in,
// so the AuthContext can flip the app back to the login screen.
let onSessionExpired: (() => void) | null = null
export function setSessionExpiredHandler(handler: (() => void) | null) {
  onSessionExpired = handler
}

let refreshInFlight: Promise<boolean> | null = null

async function doRefresh(): Promise<boolean> {
  const refreshToken = tokenStore.getRefresh()
  if (!refreshToken) return false

  const res = await fetch('/auth/refresh', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ token: refreshToken }),
  })
  if (!res.ok) {
    tokenStore.clear()
    return false
  }
  tokenStore.set((await res.json()) as AuthTokens)
  return true
}

/** Rotates the refresh token; concurrent callers share one request. */
export function refreshSession(): Promise<boolean> {
  refreshInFlight ??= doRefresh().finally(() => {
    refreshInFlight = null
  })
  return refreshInFlight
}

/** Returns an access token with at least 30 s of life left, refreshing if needed. */
export async function getValidAccessToken(): Promise<string | null> {
  const token = tokenStore.getAccess()
  if (token) {
    const exp = decodeJwt(token)?.exp
    if (typeof exp === 'number' && exp * 1000 > Date.now() + 30_000) return token
  }
  return (await refreshSession()) ? tokenStore.getAccess() : null
}

interface RequestOptions {
  method?: string
  body?: unknown
  /** Set false for the anonymous /auth endpoints. */
  auth?: boolean
}

export async function api<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { method = 'GET', body, auth = true } = options

  const send = async (): Promise<Response> => {
    const headers: Record<string, string> = {}
    if (body !== undefined) headers['Content-Type'] = 'application/json'
    if (auth) {
      const token = await getValidAccessToken()
      if (token) headers.Authorization = `Bearer ${token}`
    }
    return fetch(path, {
      method,
      headers,
      body: body !== undefined ? JSON.stringify(body) : undefined,
    })
  }

  let res = await send()

  // A 401 despite a fresh-looking token means it was revoked server-side:
  // force one refresh and retry before giving up on the session.
  if (res.status === 401 && auth) {
    if (await refreshSession()) res = await send()
    if (res.status === 401) {
      tokenStore.clear()
      onSessionExpired?.()
    }
  }

  if (!res.ok) {
    let detail = `Request failed (${res.status})`
    let errors: string[] = []
    try {
      const problem = (await res.json()) as ApiErrorBody
      detail = problem.detail || detail
      errors = problem.errors ?? []
    } catch {
      // non-JSON error body; keep the generic message
    }
    throw new ApiError(res.status, detail, errors)
  }

  if (res.status === 204) return undefined as T
  return (await res.json()) as T
}
