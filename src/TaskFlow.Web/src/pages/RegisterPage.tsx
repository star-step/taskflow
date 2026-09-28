import { useState, type FormEvent } from 'react';
import { Link, Navigate, useNavigate, useSearchParams } from 'react-router';
import { ApiError } from '../api/client';
import { LIMITS } from '../api/types';
import { useAuth } from '../auth/AuthProvider';
import { StatusIcon } from '../components/Badges';
import { AlertIcon, EyeIcon } from '../components/Icons';
import ui from '../components/ui.module.css';
import { AuthLayout } from './AuthLayout';
import { safeReturnTo } from './LoginPage';
import { passwordChecks } from './passwordRules';
import styles from './authForm.module.css';

type Field = 'displayName' | 'email' | 'password' | 'confirm';
type Errors = Partial<Record<Field, string>>;

export function RegisterPage() {
  const { register, status } = useAuth();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const returnTo = safeReturnTo(params.get('returnTo'));

  const [values, setValues] = useState({ displayName: '', email: '', password: '', confirm: '' });
  const [showPassword, setShowPassword] = useState(false);
  const [errors, setErrors] = useState<Errors>({});
  const [submitted, setSubmitted] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [emailTaken, setEmailTaken] = useState(false);
  const [busy, setBusy] = useState(false);

  if (status === 'authenticated') return <Navigate to={returnTo} replace />;

  const checks = passwordChecks(values.password, values.email, values.displayName);
  const matches = values.confirm.length > 0 && values.confirm === values.password;

  function validate(v = values): Errors {
    const e: Errors = {};
    const name = v.displayName.trim();
    if (!name) e.displayName = 'Enter your full name.';
    else if (name.length < LIMITS.displayNameMin || name.length > LIMITS.displayNameMax) e.displayName = 'Full name must be between 2 and 100 characters.';
    if (!v.email.trim()) e.email = 'Enter your work email.';
    else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v.email.trim())) e.email = 'Enter a valid email address.';
    const c = passwordChecks(v.password, v.email, v.displayName);
    if (!v.password) e.password = 'Choose a password.';
    else if (!c.length) e.password = `Password must be at least ${LIMITS.passwordMin} characters.`;
    else if (!c.personal) e.password = "Password can't include your name or email.";
    if (v.confirm !== v.password) e.confirm = "Passwords don't match.";
    return e;
  }

  function set(field: Field, value: string) {
    const next = { ...values, [field]: value };
    setValues(next);
    if (field === 'email') setEmailTaken(false);
    // After the first submit, errors update as the user types.
    if (submitted) setErrors(validate(next));
  }

  async function submit(e: FormEvent) {
    e.preventDefault();
    setSubmitted(true);
    const found = validate();
    setErrors(found);
    if (Object.keys(found).length > 0) return;

    setBusy(true);
    setFormError(null);
    try {
      await register(values.displayName.trim(), values.email.trim(), values.password);
      navigate(returnTo, { replace: true });
    } catch (err) {
      if (err instanceof ApiError) {
        setEmailTaken(err.errorCode === 'EMAIL_TAKEN');
        setErrors({
          displayName: err.fieldError('displayName'),
          email: err.fieldError('email'),
          password: err.fieldError('password'),
        });
        if (!err.errors || Object.keys(err.errors).length === 0) setFormError(err.message);
      } else {
        setFormError('Registration failed. Try again.');
      }
    } finally {
      setBusy(false);
    }
  }

  const input = (field: Field) => `${ui.input} ${errors[field] ? ui.invalid : ''}`;
  const describedBy = (field: Field, extra?: string) =>
    [errors[field] ? `${field}-error` : null, extra].filter(Boolean).join(' ') || undefined;
  const error = (field: Field) => errors[field] && (
    <span id={`${field}-error`} className={ui.error}>
      <AlertIcon />
      {errors[field]}
      {field === 'email' && emailTaken && (
        <> <Link className={ui.link} to={`/login?email=${encodeURIComponent(values.email.trim())}`}>Sign in instead</Link></>
      )}
    </span>
  );
  const rule = (ok: boolean, text: string) => (
    <li className={ok ? styles.ok : undefined}><StatusIcon status={ok ? 'Done' : 'ToDo'} /><span>{text}</span></li>
  );

  return (
    <AuthLayout onSubmit={submit}>
      <div>
        <h1 className={styles.heading}>Create your account</h1>
        <p className={styles.lead}>It takes a minute. You'll be signed in straight after.</p>
      </div>
      {formError && <div className={ui.alert} role="alert"><AlertIcon /><span>{formError}</span></div>}

      <div className={ui.field}>
        <label className={ui.label} htmlFor="displayName">Full name</label>
        <input id="displayName" autoComplete="name" className={input('displayName')} value={values.displayName}
          onChange={(e) => set('displayName', e.target.value)} aria-invalid={!!errors.displayName}
          aria-describedby={describedBy('displayName')} maxLength={LIMITS.displayNameMax} autoFocus />
        {error('displayName')}
      </div>

      <div className={ui.field}>
        <label className={ui.label} htmlFor="email">Work email</label>
        <input id="email" type="email" autoComplete="email" className={input('email')} value={values.email}
          onChange={(e) => set('email', e.target.value)} aria-invalid={!!errors.email}
          aria-describedby={describedBy('email', 'email-hint')} placeholder="name@company.com" />
        {error('email') || <span id="email-hint" className={ui.hint}><span>Use your company email address</span></span>}
      </div>

      <div className={ui.field}>
        <label className={ui.label} htmlFor="password">Password</label>
        <div className={styles.pw}>
          <input id="password" type={showPassword ? 'text' : 'password'} autoComplete="new-password"
            className={input('password')} value={values.password} onChange={(e) => set('password', e.target.value)}
            aria-invalid={!!errors.password} aria-describedby={describedBy('password', 'password-rules')} />
          <button type="button" className={`${ui.iconBtn} ${styles.reveal}`} onClick={() => setShowPassword((v) => !v)}
            aria-label={showPassword ? 'Hide password' : 'Show password'} aria-pressed={showPassword}>
            <EyeIcon off={showPassword} />
          </button>
        </div>
        <ul id="password-rules" className={styles.rules} aria-live="polite">
          {rule(checks.length, `At least ${LIMITS.passwordMin} characters`)}
          {rule(checks.personal, "Doesn't include your name or email")}
          {rule(matches, 'Passwords match')}
        </ul>
        {error('password')}
      </div>

      <div className={ui.field}>
        <label className={ui.label} htmlFor="confirm">Confirm password</label>
        <input id="confirm" type={showPassword ? 'text' : 'password'} autoComplete="new-password" className={input('confirm')}
          value={values.confirm} onChange={(e) => set('confirm', e.target.value)} aria-invalid={!!errors.confirm}
          aria-describedby={describedBy('confirm')} />
        {error('confirm')}
      </div>

      <button type="submit" className={`${ui.btn} ${ui.primary} ${ui.block}`} disabled={busy}>
        {busy ? 'Creating account…' : 'Create account'}
      </button>
      <p className={styles.note}>
        Already have an account? <Link className={ui.link} to={`/login${params.toString() ? `?${params}` : ''}`}>Sign in</Link>
      </p>
    </AuthLayout>
  );
}
