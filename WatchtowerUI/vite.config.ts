import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// The API has no CORS policy, so the dev server proxies every backend route
// (including the SignalR websocket) to keep requests same-origin.
const apiTarget = 'http://localhost:5220'

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/auth': apiTarget,
      '/endpoints': apiTarget,
      '/dashboard': apiTarget,
      '/health': apiTarget,
      '/hubs': { target: apiTarget, ws: true },
    },
  },
})
