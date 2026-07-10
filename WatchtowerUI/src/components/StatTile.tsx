import type { ReactNode } from 'react'

interface Props {
  label: string
  value: ReactNode
  sub?: ReactNode
  /** Optional status accent shown as a colored dot beside the value. */
  tone?: 'good' | 'warning' | 'critical' | 'muted'
}

export function StatTile({ label, value, sub, tone }: Props) {
  return (
    <div className="stat-tile">
      <div className="stat-label">{label}</div>
      <div className="stat-value">
        {tone && <span className={`status-dot tone-${tone}`} aria-hidden />}
        {value}
      </div>
      {sub && <div className="stat-sub">{sub}</div>}
    </div>
  )
}
