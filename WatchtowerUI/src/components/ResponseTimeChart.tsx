import {
  Area,
  AreaChart,
  CartesianGrid,
  ReferenceLine,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import type { CheckResult } from '../api/types'
import { fmtDateTime, fmtMs, fmtTime } from '../lib/format'

// Chart chrome tokens (dark surface) — see the dataviz palette reference.
const ACCENT = '#3987e5'
const CRITICAL = '#d03b3b'
const WARNING = '#fab219'
const SURFACE = '#1f1f1e'
const GRID = '#2c2c2a'
const AXIS = '#383835'
const MUTED = '#898781'

interface Props {
  /** Checks in chronological order. */
  checks: CheckResult[]
  /** Optional SLA threshold drawn as a reference line. */
  maxResponseTimeMs?: number | null
}

interface Point extends CheckResult {
  label: string
}

// Failed checks get a marker in the critical status color (with a surface ring
// so it stays legible on the line); successful points stay unmarked.
// eslint-disable-next-line @typescript-eslint/no-explicit-any
function FailureDot(props: any) {
  const { key, cx, cy, payload } = props
  if (payload.isSuccess || cx == null || cy == null) return <g key={key} />
  return <circle key={key} cx={cx} cy={cy} r={4} fill={CRITICAL} stroke={SURFACE} strokeWidth={2} />
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
function ChartTooltip({ active, payload }: any) {
  if (!active || !payload?.length) return null
  const check = payload[0].payload as Point
  return (
    <div className="chart-tooltip">
      <div className="chart-tooltip-title">{fmtDateTime(check.checkedAt)}</div>
      <div>{fmtMs(check.responseTimeMs)}</div>
      <div className={check.isSuccess ? 'text-good' : 'text-critical'}>
        {check.isSuccess ? '✓ success' : '⚠ failed'}
        {check.statusCode != null && ` · HTTP ${check.statusCode}`}
      </div>
      {check.errorMessage && <div className="chart-tooltip-error">{check.errorMessage}</div>}
    </div>
  )
}

export function ResponseTimeChart({ checks, maxResponseTimeMs }: Props) {
  if (checks.length === 0) {
    return <div className="empty-state">No checks recorded yet.</div>
  }

  const data: Point[] = checks.map((c) => ({ ...c, label: fmtTime(c.checkedAt) }))

  return (
    <div className="chart-frame">
      <ResponsiveContainer width="100%" height={260}>
        <AreaChart data={data} margin={{ top: 8, right: 12, bottom: 0, left: 0 }}>
          <CartesianGrid vertical={false} stroke={GRID} strokeWidth={1} />
          <XAxis
            dataKey="label"
            tick={{ fill: MUTED, fontSize: 11 }}
            tickLine={false}
            axisLine={{ stroke: AXIS }}
            minTickGap={48}
          />
          <YAxis
            tick={{ fill: MUTED, fontSize: 11 }}
            tickLine={false}
            axisLine={false}
            width={56}
            tickFormatter={(v: number) => `${v.toLocaleString()} ms`}
          />
          <Tooltip content={<ChartTooltip />} cursor={{ stroke: MUTED, strokeWidth: 1 }} />
          {maxResponseTimeMs != null && (
            <ReferenceLine
              y={maxResponseTimeMs}
              stroke={WARNING}
              strokeWidth={1}
              label={{ value: 'SLA', fill: MUTED, fontSize: 11, position: 'insideTopRight' }}
            />
          )}
          <Area
            type="monotone"
            dataKey="responseTimeMs"
            stroke={ACCENT}
            strokeWidth={2}
            fill={ACCENT}
            fillOpacity={0.1}
            dot={FailureDot}
            activeDot={{ r: 4, fill: ACCENT, stroke: SURFACE, strokeWidth: 2 }}
            isAnimationActive={false}
          />
        </AreaChart>
      </ResponsiveContainer>
    </div>
  )
}
