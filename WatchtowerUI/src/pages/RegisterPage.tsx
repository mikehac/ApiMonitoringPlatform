import { useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { authApi } from '../api/auth'
import { ApiError } from '../api/http'

/**
 * Two-step screen: register, then verify the email using the dev-stub token
 * the API returns (a real deployment would email it instead).
 */
export function RegisterPage() {
  const [displayName, setDisplayName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [errors, setErrors] = useState<string[]>([])
  const [busy, setBusy] = useState(false)

  const [verifyToken, setVerifyToken] = useState<string | null>(null)
  const [verified, setVerified] = useState(false)

  const fail = (err: unknown) => {
    if (err instanceof ApiError) {
      setError(err.message)
      setErrors(err.errors)
    } else {
      setError('Could not reach the server.')
      setErrors([])
    }
  }

  const onRegister = async (e: FormEvent) => {
    e.preventDefault()
    setError(null)
    setErrors([])
    setBusy(true)
    try {
      const result = await authApi.register(email, password, displayName)
      setVerifyToken(result.emailVerificationToken)
    } catch (err) {
      fail(err)
    } finally {
      setBusy(false)
    }
  }

  const onVerify = async (e: FormEvent) => {
    e.preventDefault()
    setError(null)
    setErrors([])
    setBusy(true)
    try {
      await authApi.verifyEmail(verifyToken!)
      setVerified(true)
    } catch (err) {
      fail(err)
    } finally {
      setBusy(false)
    }
  }

  if (verified) {
    return (
      <div className="auth-page">
        <div className="card auth-card">
          <h1>Email verified ✓</h1>
          <p className="auth-subtitle">Your account is ready.</p>
          <Link className="btn btn-primary btn-block" to="/login">
            Go to sign in
          </Link>
        </div>
      </div>
    )
  }

  if (verifyToken !== null) {
    return (
      <div className="auth-page">
        <form className="card auth-card" onSubmit={onVerify}>
          <h1>Verify your email</h1>
          <p className="auth-subtitle">
            In development the verification token is returned directly instead of being emailed.
          </p>

          {error && <div className="form-error">{error}</div>}

          <label className="field">
            <span>Verification token</span>
            <input
              value={verifyToken}
              onChange={(e) => setVerifyToken(e.target.value)}
              className="mono"
              required
            />
          </label>

          <button className="btn btn-primary btn-block" disabled={busy}>
            {busy ? 'Verifying…' : 'Verify email'}
          </button>
        </form>
      </div>
    )
  }

  return (
    <div className="auth-page">
      <form className="card auth-card" onSubmit={onRegister}>
        <h1>Create account</h1>
        <p className="auth-subtitle">Start monitoring your endpoints</p>

        {error && <div className="form-error">{error}</div>}
        {errors.length > 0 && (
          <ul className="form-error-list">
            {errors.map((msg) => (
              <li key={msg}>{msg}</li>
            ))}
          </ul>
        )}

        <label className="field">
          <span>Display name</span>
          <input
            value={displayName}
            onChange={(e) => setDisplayName(e.target.value)}
            autoComplete="name"
            required
          />
        </label>
        <label className="field">
          <span>Email</span>
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            autoComplete="email"
            required
          />
        </label>
        <label className="field">
          <span>Password</span>
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            autoComplete="new-password"
            minLength={8}
            required
          />
        </label>

        <button className="btn btn-primary btn-block" disabled={busy}>
          {busy ? 'Creating…' : 'Create account'}
        </button>

        <div className="auth-links">
          <Link to="/login">Already have an account? Sign in</Link>
        </div>
      </form>
    </div>
  )
}
