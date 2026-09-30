import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router';
import { useAuth } from './authContext';
import styles from './RequireAuth.module.css';

/** Shows children to signed-in users; otherwise sends them to sign in and back here afterwards. */
export function RequireAuth({ children }: { children: ReactNode }) {
  const { status, retry } = useAuth();
  const location = useLocation();

  if (status === 'loading') {
    return <div className={styles.splash} role="status" aria-live="polite"><span className={styles.dot} />Loading TaskFlow…</div>;
  }
  if (status === 'unavailable') {
    return (
      <div className={styles.splash} role="alert">
        <span className={styles.dot} />
        <p className={styles.message}>Can't reach TaskFlow right now. Your session is kept; we'll keep trying.</p>
        <button type="button" className={styles.retry} onClick={retry}>Try again</button>
      </div>
    );
  }
  if (status === 'anonymous') {
    const returnTo = location.pathname + location.search;
    return <Navigate to={`/login${returnTo !== '/' ? `?returnTo=${encodeURIComponent(returnTo)}` : ''}`} replace />;
  }
  return <>{children}</>;
}
