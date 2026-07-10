import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

export function Layout() {
  const { state, logout } = useAuth()

  return (
    <div className="app-shell">
      <header className="app-header">
        <div className="app-header-inner">
          <NavLink to="/" className="brand">
            <img src="/watchtower.svg" alt="" width={24} height={24} />
            Watchtower
          </NavLink>
          <nav className="app-nav">
            <NavLink to="/" end>
              Dashboard
            </NavLink>
            <NavLink to="/endpoints">Endpoints</NavLink>
          </nav>
          <div className="app-user">
            {state.status === 'signedIn' && <span className="app-user-email">{state.email}</span>}
            <button className="btn btn-ghost" onClick={logout}>
              Sign out
            </button>
          </div>
        </div>
      </header>
      <main className="app-main">
        <Outlet />
      </main>
    </div>
  )
}
