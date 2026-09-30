import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router';
import { useAuth } from './authContext';
import styles from './RequireAuth.module.css';

/** Shows children to signed-in users; otherwise sends them to sign in and back here afterwards. */
export function RequireAuth({ children }: { children: ReactNode }) {
  const { status } = useAuth();
  const location = useLocation();

  if (status === 'loading') {
    return <div className={styles.splash} role="status" aria-live="polite"><span className={styles.dot} />Loading TaskFlow…</div>;
  }
  if (status === 'anonymous') {
    const returnTo = location.pathname + location.search;
    return <Navigate to={`/login${returnTo !== '/' ? `?returnTo=${encodeURIComponent(returnTo)}` : ''}`} replace />;
  }
  return <>{children}</>;
}
