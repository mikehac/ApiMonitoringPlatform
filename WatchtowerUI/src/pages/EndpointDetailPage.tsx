import { useState } from 'react'
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { watchtowerApi } from '../api/watchtower'
import type { EndpointDetail } from '../api/types'
import { Pagination } from '../components/Pagination'
import { ResponseTimeChart } from '../components/ResponseTimeChart'
import { StatTile } from '../components/StatTile'
import { StatusBadge } from '../components/StatusBadge'
import { useToasts } from '../components/Toasts'
import { fmtDateTime, fmtDuration, fmtInterval, fmtMs, timeAgo } from '../lib/format'

type Tab = 'overview' | 'checks' | 'alerts'
type Range = '24h' | '7d' | '30d'

const RANGE_HOURS: Record<Range, number> = { '24h': 24, '7d': 168, '30d': 720 }

/** Newest N checks fetched for the response-time chart (the API pages newest-first). */
const CHART_POINTS = 100

export function EndpointDetailPage() {
  const { id = '' } = useParams()
  const [tab, setTab] = useState<Tab>('overview')

  const endpoint = useQuery({
    queryKey: ['endpoints', id],
    queryFn: () => watchtowerApi.endpoint(id),
  })

  if (endpoint.isPending) return <div className="empty-state">Loading endpoint…</div>
  if (endpoint.isError) return <div className="empty-state">Endpoint not found.</div>

  const e = endpoint.data

  return (
    <>
      <div className="page-head">
        <div>
          <div className="breadcrumb">
            <Link to="/endpoints">Endpoints</Link> /
          </div>
          <h1 className="page-title-row">
            {e.name} <StatusBadge status={e.status} isActive={e.isActive} />
          </h1>
          <div className="page-subtitle">
            <span className="method-chip">{e.httpMethod}</span>{' '}
            <span className="mono">{e.url}</span>
          </div>
        </div>
        <HeaderActions endpoint={e} />
      </div>

      <div className="tabs" role="tablist">
        {(['overview', 'checks', 'alerts'] as const).map((t) => (
          <button
            key={t}
            role="tab"
            aria-selected={tab === t}
            className={`tab ${tab === t ? 'tab-active' : ''}`}
            onClick={() => setTab(t)}
          >
            {t[0].toUpperCase() + t.slice(1)}
          </button>
        ))}
      </div>

      {tab === 'overview' && <OverviewTab endpoint={e} />}
      {tab === 'checks' && <ChecksTab endpointId={id} />}
      {tab === 'alerts' && <AlertsTab endpointId={id} />}
    </>
  )
}

function HeaderActions({ endpoint }: { endpoint: EndpointDetail }) {
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const { push } = useToasts()

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ['endpoints'] })
    void queryClient.invalidateQueries({ queryKey: ['dashboard'] })
  }

  const toggle = useMutation({
    mutationFn: () => watchtowerApi.toggleEndpoint(endpoint.id),
    onSuccess: (result) => {
      invalidate()
      push('info', `${endpoint.name} ${result.isActive ? 'resumed' : 'paused'}`)
    },
    onError: () => push('error', 'Could not toggle the endpoint'),
  })

  const remove = useMutation({
    mutationFn: () => watchtowerApi.deleteEndpoint(endpoint.id),
    onSuccess: () => {
      invalidate()
      push('info', 'Endpoint deleted')
      navigate('/endpoints')
    },
    onError: () => push('error', 'Could not delete the endpoint'),
  })

  return (
    <div className="page-actions">
      <button className="btn" onClick={() => toggle.mutate()} disabled={toggle.isPending}>
        {endpoint.isActive ? 'Pause' : 'Resume'}
      </button>
      <Link className="btn" to={`/endpoints/${endpoint.id}/edit`}>
        Edit
      </Link>
      <button
        className="btn btn-danger"
        onClick={() => {
          if (window.confirm(`Delete "${endpoint.name}"?`)) remove.mutate()
        }}
        disabled={remove.isPending}
      >
        Delete
      </button>
    </div>
  )
}

function OverviewTab({ endpoint }: { endpoint: EndpointDetail }) {
  const [range, setRange] = useState<Range>('24h')

  const stats = useQuery({
    queryKey: ['endpoints', endpoint.id, 'stats', range],
    queryFn: () => {
      const to = new Date()
      const from = new Date(to.getTime() - RANGE_HOURS[range] * 3_600_000)
      return watchtowerApi.checkStats(endpoint.id, from, to)
    },
  })

  const recentChecks = useQuery({
    queryKey: ['endpoints', endpoint.id, 'checks', 1, CHART_POINTS],
    queryFn: () => watchtowerApi.checks(endpoint.id, 1, CHART_POINTS),
  })

  // API returns newest-first; the chart wants chronological order.
  const chartChecks = [...(recentChecks.data?.items ?? [])].reverse()

  return (
    <>
      <div className="range-picker" role="group" aria-label="Stats time range">
        {(['24h', '7d', '30d'] as const).map((r) => (
          <button
            key={r}
            className={`range-option ${range === r ? 'range-active' : ''}`}
            onClick={() => setRange(r)}
          >
            {r}
          </button>
        ))}
      </div>

      <div className="tile-grid">
        <StatTile
          label={`Uptime (${range})`}
          value={stats.data?.uptimePercent != null ? `${stats.data.uptimePercent}%` : '—'}
          tone={
            stats.data?.uptimePercent == null
              ? undefined
              : stats.data.uptimePercent >= 99
                ? 'good'
                : stats.data.uptimePercent >= 95
                  ? 'warning'
                  : 'critical'
          }
        />
        <StatTile
          label="Avg response"
          value={stats.data?.avgResponseTimeMs != null ? fmtMs(stats.data.avgResponseTimeMs) : '—'}
        />
        <StatTile
          label="p95 response"
          value={stats.data?.p95ResponseTimeMs != null ? fmtMs(stats.data.p95ResponseTimeMs) : '—'}
        />
        <StatTile
          label="Checks"
          value={stats.data?.totalChecks ?? '—'}
          sub={stats.data ? `${stats.data.failedChecks} failed` : undefined}
        />
      </div>

      <section className="card">
        <h2>Response time — last {CHART_POINTS} checks</h2>
        {recentChecks.isPending ? (
          <div className="empty-state">Loading checks…</div>
        ) : (
          <ResponseTimeChart checks={chartChecks} maxResponseTimeMs={endpoint.maxResponseTimeMs} />
        )}
      </section>

      <section className="card">
        <h2>Configuration</h2>
        <dl className="config-grid">
          <div>
            <dt>Check interval</dt>
            <dd>{fmtInterval(endpoint.checkIntervalSeconds)}</dd>
          </div>
          <div>
            <dt>Timeout</dt>
            <dd>{endpoint.timeoutSeconds}s</dd>
          </div>
          <div>
            <dt>Expected status</dt>
            <dd>{endpoint.expectedStatusCode ?? 'any'}</dd>
          </div>
          <div>
            <dt>Body must contain</dt>
            <dd>{endpoint.expectedBodyContains || '—'}</dd>
          </div>
          <div>
            <dt>Max response time</dt>
            <dd>{endpoint.maxResponseTimeMs != null ? fmtMs(endpoint.maxResponseTimeMs) : '—'}</dd>
          </div>
          <div>
            <dt>Last checked</dt>
            <dd>{endpoint.lastCheckedAt ? timeAgo(endpoint.lastCheckedAt) : 'never'}</dd>
          </div>
          <div>
            <dt>Created</dt>
            <dd>{fmtDateTime(endpoint.createdAt)}</dd>
          </div>
        </dl>
      </section>
    </>
  )
}

function ChecksTab({ endpointId }: { endpointId: string }) {
  const [page, setPage] = useState(1)

  const checks = useQuery({
    queryKey: ['endpoints', endpointId, 'checks', page, 20],
    queryFn: () => watchtowerApi.checks(endpointId, page),
    placeholderData: keepPreviousData,
  })

  if (checks.isPending) return <div className="empty-state">Loading checks…</div>
  if (checks.isError) return <div className="empty-state">Could not load checks.</div>

  const { items, totalPages } = checks.data

  return (
    <section className="card">
      <h2>Check history</h2>
      {items.length === 0 ? (
        <div className="empty-state">No checks recorded yet.</div>
      ) : (
        <>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Checked at</th>
                  <th>Result</th>
                  <th>HTTP status</th>
                  <th>Response time</th>
                  <th>Error</th>
                </tr>
              </thead>
              <tbody>
                {items.map((check) => (
                  <tr key={check.id}>
                    <td>{fmtDateTime(check.checkedAt)}</td>
                    <td>
                      <span className={check.isSuccess ? 'text-good' : 'text-critical'}>
                        {check.isSuccess ? '✓ success' : '⚠ failed'}
                      </span>
                    </td>
                    <td>{check.statusCode ?? '—'}</td>
                    <td>{fmtMs(check.responseTimeMs)}</td>
                    <td className="cell-secondary">{check.errorMessage ?? '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <Pagination page={page} totalPages={totalPages} onChange={setPage} />
        </>
      )}
    </section>
  )
}

function AlertsTab({ endpointId }: { endpointId: string }) {
  const [page, setPage] = useState(1)

  const alerts = useQuery({
    queryKey: ['endpoints', endpointId, 'alerts', page],
    queryFn: () => watchtowerApi.alerts(endpointId, page),
    placeholderData: keepPreviousData,
  })

  if (alerts.isPending) return <div className="empty-state">Loading alerts…</div>
  if (alerts.isError) return <div className="empty-state">Could not load alerts.</div>

  const { items, totalPages } = alerts.data

  return (
    <section className="card">
      <h2>Alert history</h2>
      {items.length === 0 ? (
        <div className="empty-state">No alerts for this endpoint.</div>
      ) : (
        <>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>State</th>
                  <th>Reason</th>
                  <th>Triggered</th>
                  <th>Resolved</th>
                  <th>Duration</th>
                </tr>
              </thead>
              <tbody>
                {items.map((alert) => (
                  <tr key={alert.id}>
                    <td>
                      <span
                        className={`status-badge ${alert.state === 'Open' ? 'status-down' : 'status-up'}`}
                      >
                        <span className="status-dot" aria-hidden />
                        {alert.state}
                      </span>
                    </td>
                    <td className="cell-secondary">{alert.reason}</td>
                    <td>{fmtDateTime(alert.triggeredAt)}</td>
                    <td>{alert.resolvedAt ? fmtDateTime(alert.resolvedAt) : '—'}</td>
                    <td>{alert.durationSeconds != null ? fmtDuration(alert.durationSeconds) : '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <Pagination page={page} totalPages={totalPages} onChange={setPage} />
        </>
      )}
    </section>
  )
}
