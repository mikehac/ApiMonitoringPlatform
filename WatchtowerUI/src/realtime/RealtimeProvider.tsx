import { useEffect, type ReactNode } from 'react'
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { useQueryClient } from '@tanstack/react-query'
import { getValidAccessToken } from '../api/http'
import type {
  AlertOpenedEvent,
  AlertResolvedEvent,
  EndpointStatusChangedEvent,
} from '../api/types'
import { useAuth } from '../auth/AuthContext'
import { useToasts } from '../components/Toasts'
import { fmtDuration } from '../lib/format'

/**
 * Holds one SignalR connection to /hubs/watchtower for the lifetime of the
 * signed-in session. Hub events drive TanStack Query cache invalidation
 * (so every mounted screen refetches) and surface alerts as toasts.
 */
export function RealtimeProvider({ children }: { children: ReactNode }) {
  const { state } = useAuth()
  const queryClient = useQueryClient()
  const { push } = useToasts()

  const signedIn = state.status === 'signedIn'

  useEffect(() => {
    if (!signedIn) return

    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/watchtower', {
        accessTokenFactory: async () => (await getValidAccessToken()) ?? '',
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    const invalidate = () => {
      void queryClient.invalidateQueries({ queryKey: ['dashboard'] })
      void queryClient.invalidateQueries({ queryKey: ['endpoints'] })
    }

    connection.on('EndpointStatusChanged', (e: EndpointStatusChangedEvent) => {
      invalidate()
      if (e.newStatus === 'Down') {
        push('error', `${e.endpointName} is down`, `Was ${e.previousStatus.toLowerCase()}`)
      } else if (e.previousStatus === 'Down') {
        push('success', `${e.endpointName} is back up`)
      }
    })

    connection.on('AlertOpened', (e: AlertOpenedEvent) => {
      invalidate()
      push('error', `Alert opened — ${e.endpointName}`, e.reason)
    })

    connection.on('AlertResolved', (e: AlertResolvedEvent) => {
      invalidate()
      push('success', `Alert resolved — ${e.endpointName}`, `Down for ${fmtDuration(e.durationSeconds)}`)
    })

    connection.start().catch((err: unknown) => {
      console.error('SignalR connection failed', err)
    })

    return () => {
      void connection.stop()
    }
  }, [signedIn, queryClient, push])

  return children
}
