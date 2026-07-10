import { api } from './http'
import type {
  Alert,
  CheckResult,
  CheckStats,
  CreateEndpointResult,
  Dashboard,
  EndpointDetail,
  EndpointInput,
  EndpointSummary,
  PagedResult,
} from './types'

export const watchtowerApi = {
  dashboard: () => api<Dashboard>('/dashboard'),

  endpoints: () => api<EndpointSummary[]>('/endpoints'),

  endpoint: (id: string) => api<EndpointDetail>(`/endpoints/${id}`),

  createEndpoint: (input: EndpointInput) =>
    api<CreateEndpointResult>('/endpoints', { method: 'POST', body: input }),

  updateEndpoint: (id: string, input: EndpointInput) =>
    api<void>(`/endpoints/${id}`, { method: 'PUT', body: input }),

  deleteEndpoint: (id: string) => api<void>(`/endpoints/${id}`, { method: 'DELETE' }),

  toggleEndpoint: (id: string) =>
    api<{ isActive: boolean }>(`/endpoints/${id}/toggle`, { method: 'PATCH' }),

  checks: (id: string, page: number, pageSize = 20) =>
    api<PagedResult<CheckResult>>(`/endpoints/${id}/checks?page=${page}&pageSize=${pageSize}`),

  checkStats: (id: string, from: Date, to: Date) =>
    api<CheckStats>(
      `/endpoints/${id}/checks/stats?from=${encodeURIComponent(from.toISOString())}&to=${encodeURIComponent(to.toISOString())}`,
    ),

  alerts: (id: string, page: number, pageSize = 20) =>
    api<PagedResult<Alert>>(`/endpoints/${id}/alerts?page=${page}&pageSize=${pageSize}`),
}
