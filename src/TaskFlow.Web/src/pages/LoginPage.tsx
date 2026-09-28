import { useState, type FormEvent } from 'react';
import { Link, Navigate, useNavigate, useSearchParams } from 'react-router';
import { ApiError } from '../api/client';
import { useAuth } from '../auth/AuthProvider';
import { AlertIcon, EyeIcon } from '../components/Icons';
import ui from '../components/ui.module.css';
import { AuthLayout } from './AuthLayout';
import styles from './authForm.module.css';

/** Only allow same-app paths as a return target, never another site. */
export function safeReturnTo(value: string | null): string {
  return value && value.startsWith('/') && !value.startsWith('//') ? value : '/';
}

export function LoginPage() {
  const { login, status } = useAuth();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const returnTo = safeReturnTo(params.get('returnTo'));

  const [email, setEmail] = useState(params.get('email') ?? '');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<{ email?: string; password?: string }>({});
  const [busy, setBusy] = useState(false);

  if (status === 'authenticated') return <Navigate to={returnTo} replace />;

  async function submit(e: FormEvent) {
    e.preventDefault();
    const errs = {
      email: email.trim() ? undefined : 'Enter your email.',
      password: password ? undefined : 'Enter your password.',
    };
    setFieldErrors(errs);
    if (errs.email || errs.password) return;

    setBusy(true);
    setError(null);
    try {
      await login(email.trim(), password);
      navigate(returnTo, { replace: true });
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Sign-in failed. Try again.');
      setPassword('');
    } finally {
      setBusy(false);
    }
  }

  return (
    <AuthLayout onSubmit={submit}>
      <div>
        <h1 className={styles.heading}>Sign in</h1>
        <p className={styles.lead}>Use your work email.</p>
      </div>
      {error && <div className={ui.alert} role="alert"><AlertIcon /><span>{error}</span></div>}
      <div className={ui.field}>
        <label className={ui.label} htmlFor="email">Email</label>
        <input id="email" type="email" autoComplete="username" placeholder="name@company.com" autoFocus
          className={`${ui.input} ${fieldErrors.email ? ui.invalid : ''}`} value={email}
          onChange={(e) => setEmail(e.target.value)} aria-invalid={!!fieldErrors.email}
          aria-describedby={fieldErrors.email ? 'email-error' : undefined} />
        {fieldErrors.email && <span id="email-error" className={ui.error}><AlertIcon />{fieldErrors.email}</span>}
      </div>
      <div className={ui.field}>
        <label className={ui.label} htmlFor="password">Password</label>
        <div className={styles.pw}>
          <input id="password" type={showPassword ? 'text' : 'password'} autoComplete="current-password"
            placeholder="Your password" className={`${ui.input} ${fieldErrors.password ? ui.invalid : ''}`}
            value={password} onChange={(e) => setPassword(e.target.value)} aria-invalid={!!fieldErrors.password}
            aria-describedby={fieldErrors.password ? 'password-error' : undefined} />
          <button type="button" className={`${ui.iconBtn} ${styles.reveal}`} onClick={() => setShowPassword((v) => !v)}
            aria-label={showPassword ? 'Hide password' : 'Show password'} aria-pressed={showPassword}>
            <EyeIcon off={showPassword} />
          </button>
        </div>
        {fieldErrors.password && <span id="password-error" className={ui.error}><AlertIcon />{fieldErrors.password}</span>}
      </div>
      <button type="submit" className={`${ui.btn} ${ui.primary} ${ui.block}`} disabled={busy}>
        {busy ? 'Signing in…' : 'Sign in'}
      </button>
      <p className={styles.note}>
        New to TaskFlow? <Link className={ui.link} to={`/register${params.toString() ? `?${params}` : ''}`}>Create an account</Link>
      </p>
    </AuthLayout>
  );
}
