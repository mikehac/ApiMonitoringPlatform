import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { authApi } from '../api/auth'
import { ApiError } from '../api/http'

export function ForgotPasswordPage() {
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [devToken, setDevToken] = useState<string | null>(null)
  const [requested, setRequested] = useState(false)

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setError(null)
    setBusy(true)
    try {
      const result = await authApi.forgotPassword(email)
      setDevToken(result.devPasswordResetToken)
      setRequested(true)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not reach the server.')
    } finally {
      setBusy(false)
    }
  }

  if (requested) {
    return (
      <div className="auth-page">
        <div className="card auth-card">
          <h1>Check your email</h1>
          <p className="auth-subtitle">
            If an account exists for <strong>{email}</strong>, a reset link has been sent.
          </p>
          {devToken && (
            <>
              <p className="auth-subtitle">
                Development stub — the reset token is returned directly:
              </p>
              <code className="token-box mono">{devToken}</code>
              <button
                className="btn btn-primary btn-block"
                onClick={() => navigate(`/reset-password?token=${encodeURIComponent(devToken)}`)}
              >
                Continue to reset password
              </button>
            </>
          )}
          <div className="auth-links">
            <Link to="/login">Back to sign in</Link>
            {!devToken && <Link to="/reset-password">I have a reset token</Link>}
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="auth-page">
      <form className="card auth-card" onSubmit={onSubmit}>
        <h1>Forgot password</h1>
        <p className="auth-subtitle">Enter your email and we'll send a reset token.</p>

        {error && <div className="form-error">{error}</div>}

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

        <button className="btn btn-primary btn-block" disabled={busy}>
          {busy ? 'Sending…' : 'Send reset token'}
        </button>

        <div className="auth-links">
          <Link to="/login">Back to sign in</Link>
        </div>
      </form>
    </div>
  )
}
