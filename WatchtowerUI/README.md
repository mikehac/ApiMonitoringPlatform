# WatchtowerUI

React front-end for the Watchtower API monitoring platform (Phase 7).

**Stack:** Vite + React 19 + TypeScript · TanStack Query · React Router · Recharts · @microsoft/signalr

## Running in development

1. Start the backend stack (PostgreSQL + Redis via `Watchtower/docker-compose.yml`, then the
   `Watchtower.Api` and `Watchtower.Worker` projects — API listens on `http://localhost:5220`).
2. Start the UI:

   ```bash
   npm install
   npm run dev
   ```

   The app runs at <http://localhost:5173>. The Vite dev server proxies `/auth`, `/endpoints`,
   `/dashboard`, `/health`, and the SignalR websocket at `/hubs` to the API, so no CORS
   configuration is needed on the backend.

## Screens

- **Login / Register** — register shows the email-verification step using the dev-stub token
  the API returns; login issues a JWT (15 min) + refresh token (7 days) with silent refresh.
- **Forgot / Reset password** — uses the dev-stub reset token flow.
- **Dashboard** — status tiles (Up / Down / Degraded / Unknown / open alerts) and the ten most
  recent incidents.
- **Endpoints** — list with status badges, pause/resume, edit and delete actions.
- **Endpoint detail** — Overview | Checks | Alerts tabs; 24h/7d/30d stats (uptime, avg, p95),
  response-time chart over the latest 100 checks, paginated check and alert history.
- **Endpoint form** — one form for both create (POST) and edit (PUT).

## Real-time

One SignalR connection to `/hubs/watchtower` per signed-in session (`RealtimeProvider`).
`EndpointStatusChanged`, `AlertOpened`, and `AlertResolved` events invalidate the TanStack
Query caches (`dashboard`, `endpoints`) so every mounted screen refetches, and alert events
surface as toasts app-wide.

## Notes

- `@rolldown/binding-win32-x64-msvc` is pinned as a devDependency because npm sometimes skips
  rolldown's optional platform binding on Windows, which breaks `vite build`.
- `npm run build` runs `tsc -b` and then a production Vite build.
