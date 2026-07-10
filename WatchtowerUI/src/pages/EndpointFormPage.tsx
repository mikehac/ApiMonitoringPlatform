import { useEffect, useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ApiError } from '../api/http'
import { watchtowerApi } from '../api/watchtower'
import type { EndpointInput } from '../api/types'
import { useToasts } from '../components/Toasts'

const HTTP_METHODS = ['GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'HEAD']

// Form state keeps numbers as strings so partially-typed input never crashes parsing.
interface FormState {
  name: string
  url: string
  httpMethod: string
  checkIntervalSeconds: string
  timeoutSeconds: string
  expectedStatusCode: string
  expectedBodyContains: string
  maxResponseTimeMs: string
}

const DEFAULTS: FormState = {
  name: '',
  url: '',
  httpMethod: 'GET',
  checkIntervalSeconds: '60',
  timeoutSeconds: '30',
  expectedStatusCode: '200',
  expectedBodyContains: '',
  maxResponseTimeMs: '',
}

function toInput(form: FormState): EndpointInput {
  return {
    name: form.name.trim(),
    url: form.url.trim(),
    httpMethod: form.httpMethod,
    checkIntervalSeconds: Number(form.checkIntervalSeconds),
    timeoutSeconds: Number(form.timeoutSeconds),
    expectedStatusCode: form.expectedStatusCode.trim() ? Number(form.expectedStatusCode) : null,
    expectedBodyContains: form.expectedBodyContains.trim() || null,
    maxResponseTimeMs: form.maxResponseTimeMs.trim() ? Number(form.maxResponseTimeMs) : null,
  }
}

/** One form serving both POST /endpoints (create) and PUT /endpoints/{id} (edit). */
export function EndpointFormPage() {
  const { id } = useParams()
  const isEdit = id !== undefined
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { push } = useToasts()

  const [form, setForm] = useState<FormState>(DEFAULTS)
  const [error, setError] = useState<string | null>(null)
  const [errors, setErrors] = useState<string[]>([])

  const existing = useQuery({
    queryKey: ['endpoints', id],
    queryFn: () => watchtowerApi.endpoint(id!),
    enabled: isEdit,
  })

  useEffect(() => {
    if (!existing.data) return
    const e = existing.data
    setForm({
      name: e.name,
      url: e.url,
      httpMethod: e.httpMethod,
      checkIntervalSeconds: String(e.checkIntervalSeconds),
      timeoutSeconds: String(e.timeoutSeconds),
      expectedStatusCode: e.expectedStatusCode != null ? String(e.expectedStatusCode) : '',
      expectedBodyContains: e.expectedBodyContains ?? '',
      maxResponseTimeMs: e.maxResponseTimeMs != null ? String(e.maxResponseTimeMs) : '',
    })
  }, [existing.data])

  const save = useMutation({
    mutationFn: async (input: EndpointInput) => {
      if (isEdit) {
        await watchtowerApi.updateEndpoint(id, input)
        return id
      }
      return (await watchtowerApi.createEndpoint(input)).id
    },
    onSuccess: (endpointId) => {
      void queryClient.invalidateQueries({ queryKey: ['endpoints'] })
      void queryClient.invalidateQueries({ queryKey: ['dashboard'] })
      push('success', isEdit ? 'Endpoint updated' : 'Endpoint created')
      navigate(`/endpoints/${endpointId}`)
    },
    onError: (err) => {
      if (err instanceof ApiError) {
        setError(err.message)
        setErrors(err.errors)
      } else {
        setError('Could not reach the server.')
        setErrors([])
      }
    },
  })

  const onSubmit = (e: FormEvent) => {
    e.preventDefault()
    setError(null)
    setErrors([])
    save.mutate(toInput(form))
  }

  const set = (key: keyof FormState) => (value: string) =>
    setForm((current) => ({ ...current, [key]: value }))

  if (isEdit && existing.isPending) return <div className="empty-state">Loading endpoint…</div>
  if (isEdit && existing.isError) return <div className="empty-state">Endpoint not found.</div>

  return (
    <>
      <div className="page-head">
        <div>
          <div className="breadcrumb">
            <Link to="/endpoints">Endpoints</Link> /
          </div>
          <h1>{isEdit ? `Edit ${existing.data?.name ?? 'endpoint'}` : 'Add endpoint'}</h1>
        </div>
      </div>

      <form className="card form-card" onSubmit={onSubmit}>
        {error && <div className="form-error">{error}</div>}
        {errors.length > 0 && (
          <ul className="form-error-list">
            {errors.map((msg) => (
              <li key={msg}>{msg}</li>
            ))}
          </ul>
        )}

        <label className="field">
          <span>Name</span>
          <input
            value={form.name}
            onChange={(e) => set('name')(e.target.value)}
            maxLength={200}
            required
          />
        </label>

        <div className="field-row">
          <label className="field field-method">
            <span>Method</span>
            <select value={form.httpMethod} onChange={(e) => set('httpMethod')(e.target.value)}>
              {HTTP_METHODS.map((m) => (
                <option key={m}>{m}</option>
              ))}
            </select>
          </label>
          <label className="field field-grow">
            <span>URL</span>
            <input
              type="url"
              value={form.url}
              onChange={(e) => set('url')(e.target.value)}
              placeholder="https://api.example.com/health"
              required
            />
          </label>
        </div>

        <div className="field-row">
          <label className="field">
            <span>Check interval (seconds)</span>
            <input
              type="number"
              min={30}
              max={86400}
              value={form.checkIntervalSeconds}
              onChange={(e) => set('checkIntervalSeconds')(e.target.value)}
              required
            />
          </label>
          <label className="field">
            <span>Timeout (seconds)</span>
            <input
              type="number"
              min={1}
              max={60}
              value={form.timeoutSeconds}
              onChange={(e) => set('timeoutSeconds')(e.target.value)}
              required
            />
          </label>
        </div>

        <h2 className="form-section">Success criteria</h2>

        <div className="field-row">
          <label className="field">
            <span>Expected HTTP status</span>
            <input
              type="number"
              min={100}
              max={599}
              value={form.expectedStatusCode}
              onChange={(e) => set('expectedStatusCode')(e.target.value)}
              placeholder="any"
            />
          </label>
          <label className="field">
            <span>Max response time (ms)</span>
            <input
              type="number"
              min={1}
              value={form.maxResponseTimeMs}
              onChange={(e) => set('maxResponseTimeMs')(e.target.value)}
              placeholder="no limit"
            />
          </label>
        </div>

        <label className="field">
          <span>Response body must contain</span>
          <input
            value={form.expectedBodyContains}
            onChange={(e) => set('expectedBodyContains')(e.target.value)}
            placeholder="optional substring"
          />
        </label>

        <div className="form-actions">
          <button className="btn btn-primary" disabled={save.isPending}>
            {save.isPending ? 'Saving…' : isEdit ? 'Save changes' : 'Create endpoint'}
          </button>
          <Link className="btn btn-ghost" to={isEdit ? `/endpoints/${id}` : '/endpoints'}>
            Cancel
          </Link>
        </div>
      </form>
    </>
  )
}
