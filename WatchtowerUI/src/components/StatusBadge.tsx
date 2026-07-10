import type { EndpointStatus } from '../api/types'

type BadgeStatus = EndpointStatus | 'Paused'

// Status is never carried by color alone: each badge pairs a colored dot with its label.
const STATUS_CLASS: Record<BadgeStatus, string> = {
  Up: 'status-up',
  Down: 'status-down',
  Degraded: 'status-degraded',
  Unknown: 'status-unknown',
  Paused: 'status-paused',
}

interface Props {
  status: EndpointStatus
  /** When false the endpoint is shown as Paused regardless of its last status. */
  isActive?: boolean
}

export function StatusBadge({ status, isActive = true }: Props) {
  const shown: BadgeStatus = isActive ? status : 'Paused'
  return (
    <span className={`status-badge ${STATUS_CLASS[shown]}`}>
      <span className="status-dot" aria-hidden />
      {shown}
    </span>
  )
}
