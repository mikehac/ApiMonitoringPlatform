// TypeScript mirrors of the Watchtower API DTOs (camelCase JSON).

export interface AuthTokens {
  accessToken: string
  refreshToken: string
  /** Expiry of the refresh token (the access token expires per its JWT `exp`). */
  expiresAt: string
}

export interface RegisterResult {
  userId: string
  email: string
  emailVerificationToken: string
}

export type EndpointStatus = 'Unknown' | 'Up' | 'Down' | 'Degraded'
export type AlertState = 'Open' | 'Resolved'

export interface EndpointSummary {
  id: string
  name: string
  url: string
  httpMethod: string
  status: EndpointStatus
  lastCheckedAt: string | null
  isActive: boolean
  checkIntervalSeconds: number
}

export interface EndpointDetail {
  id: string
  name: string
  url: string
  httpMethod: string
  checkIntervalSeconds: number
  timeoutSeconds: number
  expectedStatusCode: number | null
  expectedBodyContains: string | null
  maxResponseTimeMs: number | null
  status: EndpointStatus
  lastCheckedAt: string | null
  isActive: boolean
  createdAt: string
  updatedAt: string
}

/** Request body for POST /endpoints and PUT /endpoints/{id}. */
export interface EndpointInput {
  name: string
  url: string
  httpMethod: string
  checkIntervalSeconds: number
  timeoutSeconds: number
  expectedStatusCode: number | null
  expectedBodyContains: string | null
  maxResponseTimeMs: number | null
}

export interface CreateEndpointResult {
  id: string
  name: string
  url: string
  status: EndpointStatus
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface CheckResult {
  id: string
  checkedAt: string
  isSuccess: boolean
  statusCode: number | null
  responseTimeMs: number
  errorMessage: string | null
}

export interface CheckStats {
  endpointId: string
  from: string
  to: string
  totalChecks: number
  successfulChecks: number
  failedChecks: number
  uptimePercent: number | null
  avgResponseTimeMs: number | null
  p95ResponseTimeMs: number | null
}

export interface Alert {
  id: string
  state: AlertState
  reason: string
  triggeredAt: string
  resolvedAt: string | null
  durationSeconds: number | null
}

export interface RecentIncident {
  alertId: string
  endpointId: string
  endpointName: string
  state: AlertState
  reason: string
  triggeredAt: string
  resolvedAt: string | null
}

export interface Dashboard {
  totalEndpoints: number
  activeEndpoints: number
  upCount: number
  downCount: number
  degradedCount: number
  unknownCount: number
  openAlerts: number
  recentIncidents: RecentIncident[]
}

// SignalR hub payloads (client methods on /hubs/watchtower).

export interface EndpointStatusChangedEvent {
  ownerId: string
  endpointId: string
  endpointName: string
  url: string
  previousStatus: EndpointStatus
  newStatus: EndpointStatus
  responseTimeMs: number
  checkedAt: string
}

export interface AlertOpenedEvent {
  ownerId: string
  alertId: string
  endpointId: string
  endpointName: string
  url: string
  reason: string
  triggeredAt: string
}

export interface AlertResolvedEvent {
  ownerId: string
  alertId: string
  endpointId: string
  endpointName: string
  url: string
  triggeredAt: string
  resolvedAt: string
  durationSeconds: number
}

/** Error body produced by the API's ExceptionHandlingMiddleware. */
export interface ApiErrorBody {
  status: number
  detail: string
  errors: string[] | null
}
