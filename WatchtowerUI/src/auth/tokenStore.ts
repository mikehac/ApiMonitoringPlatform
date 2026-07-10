import type { AuthTokens } from '../api/types'

// The short-lived access token lives only in memory; the refresh token is
// persisted so a page reload can silently re-authenticate.
const REFRESH_KEY = 'watchtower.refreshToken'

let accessToken: string | null = null

export const tokenStore = {
  getAccess: () => accessToken,
  getRefresh: () => localStorage.getItem(REFRESH_KEY),
  set(tokens: AuthTokens) {
    accessToken = tokens.accessToken
    localStorage.setItem(REFRESH_KEY, tokens.refreshToken)
  },
  clear() {
    accessToken = null
    localStorage.removeItem(REFRESH_KEY)
  },
}

interface JwtPayload {
  sub?: string
  email?: string
  exp?: number
  [claim: string]: unknown
}

export function decodeJwt(token: string): JwtPayload | null {
  try {
    const base64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')
    return JSON.parse(atob(base64)) as JwtPayload
  } catch {
    return null
  }
}

export function currentUserEmail(): string | null {
  const token = accessToken
  if (!token) return null
  return decodeJwt(token)?.email ?? null
}
