import { api } from './http'
import type { AuthTokens, RegisterResult } from './types'

export const authApi = {
  login: (email: string, password: string) =>
    api<AuthTokens>('/auth/login', { method: 'POST', body: { email, password }, auth: false }),

  register: (email: string, password: string, displayName: string) =>
    api<RegisterResult>('/auth/register', {
      method: 'POST',
      body: { email, password, displayName },
      auth: false,
    }),

  verifyEmail: (token: string) =>
    api<void>('/auth/verify-email', { method: 'POST', body: { token }, auth: false }),

  forgotPassword: (email: string) =>
    api<{ devPasswordResetToken: string | null }>('/auth/forgot-password', {
      method: 'POST',
      body: { email },
      auth: false,
    }),

  resetPassword: (token: string, newPassword: string) =>
    api<void>('/auth/reset-password', { method: 'POST', body: { token, newPassword }, auth: false }),
}
