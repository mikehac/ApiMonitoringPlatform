import { createContext, useCallback, useContext, useRef, useState, type ReactNode } from 'react'

export type ToastKind = 'info' | 'success' | 'error'

interface Toast {
  id: number
  kind: ToastKind
  title: string
  message?: string
}

interface ToastContextValue {
  push: (kind: ToastKind, title: string, message?: string) => void
}

const ToastContext = createContext<ToastContextValue | null>(null)

const TOAST_LIFETIME_MS = 6000

const KIND_ICON: Record<ToastKind, string> = {
  info: 'ℹ',
  success: '✓',
  error: '⚠',
}

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([])
  const nextId = useRef(1)

  const dismiss = useCallback((id: number) => {
    setToasts((current) => current.filter((t) => t.id !== id))
  }, [])

  const push = useCallback(
    (kind: ToastKind, title: string, message?: string) => {
      const id = nextId.current++
      setToasts((current) => [...current, { id, kind, title, message }])
      setTimeout(() => dismiss(id), TOAST_LIFETIME_MS)
    },
    [dismiss],
  )

  return (
    <ToastContext.Provider value={{ push }}>
      {children}
      <div className="toast-stack" role="status" aria-live="polite">
        {toasts.map((toast) => (
          <div key={toast.id} className={`toast toast-${toast.kind}`}>
            <span className="toast-icon" aria-hidden>
              {KIND_ICON[toast.kind]}
            </span>
            <div className="toast-body">
              <div className="toast-title">{toast.title}</div>
              {toast.message && <div className="toast-message">{toast.message}</div>}
            </div>
            <button className="toast-close" onClick={() => dismiss(toast.id)} aria-label="Dismiss">
              ×
            </button>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  )
}

export function useToasts(): ToastContextValue {
  const ctx = useContext(ToastContext)
  if (!ctx) throw new Error('useToasts must be used within ToastProvider')
  return ctx
}
