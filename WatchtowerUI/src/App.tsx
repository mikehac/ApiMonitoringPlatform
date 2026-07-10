import type { ReactNode } from 'react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AuthProvider, useAuth } from './auth/AuthContext'
import { Layout } from './components/Layout'
import { ToastProvider } from './components/Toasts'
import { DashboardPage } from './pages/DashboardPage'
import { EndpointDetailPage } from './pages/EndpointDetailPage'
import { EndpointFormPage } from './pages/EndpointFormPage'
import { EndpointsPage } from './pages/EndpointsPage'
import { ForgotPasswordPage } from './pages/ForgotPasswordPage'
import { LoginPage } from './pages/LoginPage'
import { RegisterPage } from './pages/RegisterPage'
import { ResetPasswordPage } from './pages/ResetPasswordPage'
import { RealtimeProvider } from './realtime/RealtimeProvider'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 15_000,
      retry: 1,
    },
  },
})

function RequireAuth({ children }: { children: ReactNode }) {
  const { state } = useAuth()
  if (state.status === 'loading') return <div className="empty-state">Signing in…</div>
  if (state.status === 'signedOut') return <Navigate to="/login" replace />
  return children
}

function PublicOnly({ children }: { children: ReactNode }) {
  const { state } = useAuth()
  if (state.status === 'loading') return <div className="empty-state">Loading…</div>
  if (state.status === 'signedIn') return <Navigate to="/" replace />
  return children
}

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <ToastProvider>
          <RealtimeProvider>
            <BrowserRouter>
              <Routes>
                <Route
                  path="/login"
                  element={
                    <PublicOnly>
                      <LoginPage />
                    </PublicOnly>
                  }
                />
                <Route
                  path="/register"
                  element={
                    <PublicOnly>
                      <RegisterPage />
                    </PublicOnly>
                  }
                />
                <Route
                  path="/forgot-password"
                  element={
                    <PublicOnly>
                      <ForgotPasswordPage />
                    </PublicOnly>
                  }
                />
                <Route
                  path="/reset-password"
                  element={
                    <PublicOnly>
                      <ResetPasswordPage />
                    </PublicOnly>
                  }
                />

                <Route
                  element={
                    <RequireAuth>
                      <Layout />
                    </RequireAuth>
                  }
                >
                  <Route path="/" element={<DashboardPage />} />
                  <Route path="/endpoints" element={<EndpointsPage />} />
                  <Route path="/endpoints/new" element={<EndpointFormPage />} />
                  <Route path="/endpoints/:id" element={<EndpointDetailPage />} />
                  <Route path="/endpoints/:id/edit" element={<EndpointFormPage />} />
                </Route>

                <Route path="*" element={<Navigate to="/" replace />} />
              </Routes>
            </BrowserRouter>
          </RealtimeProvider>
        </ToastProvider>
      </AuthProvider>
    </QueryClientProvider>
  )
}
