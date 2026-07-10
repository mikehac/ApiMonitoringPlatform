import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { watchtowerApi } from '../api/watchtower'
import { StatusBadge } from '../components/StatusBadge'
import { useToasts } from '../components/Toasts'
import { fmtInterval, timeAgo } from '../lib/format'

export function EndpointsPage() {
  const queryClient = useQueryClient()
  const { push } = useToasts()

  const { data, isPending, isError } = useQuery({
    queryKey: ['endpoints'],
    queryFn: watchtowerApi.endpoints,
  })

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ['endpoints'] })
    void queryClient.invalidateQueries({ queryKey: ['dashboard'] })
  }

  const toggle = useMutation({
    mutationFn: watchtowerApi.toggleEndpoint,
    onSuccess: (result, id) => {
      invalidate()
      const name = data?.find((e) => e.id === id)?.name ?? 'Endpoint'
      push('info', `${name} ${result.isActive ? 'resumed' : 'paused'}`)
    },
    onError: () => push('error', 'Could not toggle the endpoint'),
  })

  const remove = useMutation({
    mutationFn: watchtowerApi.deleteEndpoint,
    onSuccess: () => {
      invalidate()
      push('info', 'Endpoint deleted')
    },
    onError: () => push('error', 'Could not delete the endpoint'),
  })

  if (isPending) return <div className="empty-state">Loading endpoints…</div>
  if (isError) return <div className="empty-state">Could not load endpoints.</div>

  return (
    <>
      <div className="page-head">
        <h1>Endpoints</h1>
        <Link className="btn btn-primary" to="/endpoints/new">
          + Add endpoint
        </Link>
      </div>

      <section className="card">
        {data.length === 0 ? (
          <div className="empty-state">
            No endpoints yet. <Link to="/endpoints/new">Add your first endpoint</Link> to start
            monitoring.
          </div>
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Status</th>
                  <th>Name</th>
                  <th>Target</th>
                  <th>Interval</th>
                  <th>Last checked</th>
                  <th className="cell-actions">Actions</th>
                </tr>
              </thead>
              <tbody>
                {data.map((endpoint) => (
                  <tr key={endpoint.id}>
                    <td>
                      <StatusBadge status={endpoint.status} isActive={endpoint.isActive} />
                    </td>
                    <td>
                      <Link to={`/endpoints/${endpoint.id}`}>{endpoint.name}</Link>
                    </td>
                    <td className="cell-secondary">
                      <span className="method-chip">{endpoint.httpMethod}</span>{' '}
                      <span className="mono url-cell" title={endpoint.url}>
                        {endpoint.url}
                      </span>
                    </td>
                    <td>{fmtInterval(endpoint.checkIntervalSeconds)}</td>
                    <td>{endpoint.lastCheckedAt ? timeAgo(endpoint.lastCheckedAt) : 'never'}</td>
                    <td className="cell-actions">
                      <button
                        className="btn btn-ghost btn-sm"
                        onClick={() => toggle.mutate(endpoint.id)}
                        disabled={toggle.isPending}
                      >
                        {endpoint.isActive ? 'Pause' : 'Resume'}
                      </button>
                      <Link className="btn btn-ghost btn-sm" to={`/endpoints/${endpoint.id}/edit`}>
                        Edit
                      </Link>
                      <button
                        className="btn btn-ghost btn-sm btn-danger"
                        onClick={() => {
                          if (window.confirm(`Delete "${endpoint.name}"?`)) remove.mutate(endpoint.id)
                        }}
                        disabled={remove.isPending}
                      >
                        Delete
                      </button>
                    </td>
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
