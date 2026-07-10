import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { watchtowerApi } from '../api/watchtower'
import { StatTile } from '../components/StatTile'
import { fmtDateTime, timeAgo } from '../lib/format'

export function DashboardPage() {
  const { data, isPending, isError } = useQuery({
    queryKey: ['dashboard'],
    queryFn: watchtowerApi.dashboard,
    refetchInterval: 60_000, // safety net; SignalR invalidation is the primary trigger
  })

  if (isPending) return <div className="empty-state">Loading dashboard…</div>
  if (isError) return <div className="empty-state">Could not load the dashboard.</div>

  return (
    <>
      <div className="page-head">
        <h1>Dashboard</h1>
        <Link className="btn btn-primary" to="/endpoints/new">
          + Add endpoint
        </Link>
      </div>

      <div className="tile-grid">
        <StatTile
          label="Endpoints"
          value={data.totalEndpoints}
          sub={`${data.activeEndpoints} active`}
        />
        <StatTile label="Up" value={data.upCount} tone="good" />
        <StatTile label="Down" value={data.downCount} tone="critical" />
        <StatTile label="Degraded" value={data.degradedCount} tone="warning" />
        <StatTile label="Unknown" value={data.unknownCount} tone="muted" />
        <StatTile
          label="Open alerts"
          value={data.openAlerts}
          tone={data.openAlerts > 0 ? 'critical' : 'good'}
        />
      </div>

      <section className="card">
        <h2>Recent incidents</h2>
        {data.recentIncidents.length === 0 ? (
          <div className="empty-state">No incidents — everything has been quiet.</div>
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>State</th>
                  <th>Endpoint</th>
                  <th>Reason</th>
                  <th>Triggered</th>
                  <th>Resolved</th>
                </tr>
              </thead>
              <tbody>
                {data.recentIncidents.map((incident) => (
                  <tr key={incident.alertId}>
                    <td>
                      <span
                        className={`status-badge ${incident.state === 'Open' ? 'status-down' : 'status-up'}`}
                      >
                        <span className="status-dot" aria-hidden />
                        {incident.state}
                      </span>
                    </td>
                    <td>
                      <Link to={`/endpoints/${incident.endpointId}`}>{incident.endpointName}</Link>
                    </td>
                    <td className="cell-secondary">{incident.reason}</td>
                    <td title={fmtDateTime(incident.triggeredAt)}>{timeAgo(incident.triggeredAt)}</td>
                    <td>{incident.resolvedAt ? fmtDateTime(incident.resolvedAt) : '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </>
  )
}
