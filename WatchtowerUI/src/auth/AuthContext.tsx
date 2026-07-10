import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import { authApi } from '../api/auth'
import { refreshSession, setSessionExpiredHandler } from '../api/http'
import { currentUserEmail, tokenStore } from './tokenStore'

export type AuthState =
  | { status: 'loading' }
  | { status: 'signedOut' }
  | { status: 'signedIn'; email: string }

interface AuthContextValue {
  state: AuthState
  login: (email: string, password: string) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>({ status: 'loading' })

  useEffect(() => {
    setSessionExpiredHandler(() => setState({ status: 'signedOut' }))

    // Silent sign-in: if a refresh token survived the reload, trade it for a session.
    void refreshSession().then((ok) =>
      setState(ok ? { status: 'signedIn', email: currentUserEmail() ?? '' } : { status: 'signedOut' }),
    )

    return () => setSessionExpiredHandler(null)
  }, [])

  const login = async (email: string, password: string) => {
    tokenStore.set(await authApi.login(email, password))
    setState({ status: 'signedIn', email: currentUserEmail() ?? email })
  }

  const logout = () => {
    tokenStore.clear()
    setState({ status: 'signedOut' })
  }

  return <AuthContext.Provider value={{ state, login, logout }}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
