import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { authApi } from '../api/auth';
import { ApiError, configureAuth } from '../api/client';
import type { AuthResponse, User } from '../api/types';
import { AuthContext, type AuthContextValue, type AuthStatus } from './authContext';

/** Refresh this long before the access token expires, so requests rarely hit a 401. */
const REFRESH_EARLY_MS = 60_000;
/** When the session can't be checked (429, 5xx, network), try again after this long (or Retry-After). */
const UNAVAILABLE_RETRY_MS = 5_000;
const MAX_RETRY_MS = 60_000;

/** Only 401/403 mean the refresh token is no good; anything else says nothing about the session. */
const sessionIsOver = (error: unknown) => error instanceof ApiError && (error.status === 401 || error.status === 403);

/**
 * Holds the access token in memory only (never localStorage). On start-up it asks the API to
 * restore the session from the httpOnly refresh cookie; afterwards it refreshes shortly before
 * the token expires. Concurrent refreshes share one request.
 * A refresh that fails with 401/403 ends the session. Any other failure (rate limit, API or database
 * down, no network) leaves it alone: the user sees "can't reach TaskFlow" and it is retried.
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const [status, setStatus] = useState<AuthStatus>('loading');
  const [user, setUser] = useState<User | null>(null);
  const tokenRef = useRef<string | null>(null);
  const refreshing = useRef<Promise<string | null> | null>(null);
  const timer = useRef<ReturnType<typeof setTimeout>>();
  /** Why the last refresh gave no token: the session is over, or it just couldn't be checked. */
  const lastFailure = useRef<{ over: boolean; retryAfterMs: number }>({ over: false, retryAfterMs: UNAVAILABLE_RETRY_MS });

  const endSession = useCallback(() => {
    clearTimeout(timer.current);
    tokenRef.current = null;
    setUser(null);
    setStatus('anonymous');
    queryClient.clear();
  }, [queryClient]);

  // accept() schedules the next refresh, and refresh() calls accept(). The ref breaks that cycle
  // so both callbacks keep honest dependency lists.
  const refreshRef = useRef<() => Promise<string | null>>(() => Promise.resolve(null));

  const accept = useCallback((res: AuthResponse) => {
    tokenRef.current = res.accessToken;
    setUser(res.user);
    setStatus('authenticated');
    clearTimeout(timer.current);
    const wait = new Date(res.expiresAt).getTime() - Date.now() - REFRESH_EARLY_MS;
    const scheduleRefresh = (delay: number) => {
      timer.current = setTimeout(() => {
        void refreshRef.current().then((t) => {
          // A temporary failure keeps the user signed in and tries again; only a rejected token ends it.
          if (!t && !lastFailure.current.over) scheduleRefresh(lastFailure.current.retryAfterMs);
        });
      }, delay);
    };
    scheduleRefresh(Math.max(wait, 5_000));
  }, []);

  const refresh = useCallback((): Promise<string | null> => {
    refreshing.current ??= authApi
      .refresh()
      .then((res) => {
        accept(res);
        return res.accessToken;
      })
      .catch((error: unknown) => {
        const over = sessionIsOver(error);
        const retryAfter = error instanceof ApiError && error.retryAfterSeconds ? error.retryAfterSeconds * 1000 : UNAVAILABLE_RETRY_MS;
        lastFailure.current = { over, retryAfterMs: Math.min(retryAfter, MAX_RETRY_MS) };
        if (over) endSession();
        return null;
      })
      .finally(() => {
        refreshing.current = null;
      });
    return refreshing.current;
  }, [accept, endSession]);

  useEffect(() => { refreshRef.current = refresh; }, [refresh]);

  /** Start-up check: restore the session, or say why it couldn't be restored. */
  const checkSession = useCallback(() => {
    clearTimeout(timer.current);
    setStatus((s) => (s === 'unavailable' ? 'loading' : s));
    void refresh().then((t) => {
      if (t) return;
      if (lastFailure.current.over) {
        setStatus('anonymous');
        return;
      }
      setStatus('unavailable');
      timer.current = setTimeout(() => checkSessionRef.current(), lastFailure.current.retryAfterMs);
    });
  }, [refresh]);
  const checkSessionRef = useRef(checkSession);
  useEffect(() => { checkSessionRef.current = checkSession; }, [checkSession]);

  useEffect(() => {
    configureAuth({ getToken: () => tokenRef.current, refresh });
    checkSession();
    return () => clearTimeout(timer.current);
  }, [refresh, checkSession]);

  const value = useMemo<AuthContextValue>(() => ({
    status,
    user,
    login: async (email, password) => accept(await authApi.login({ email, password })),
    register: async (displayName, email, password) => accept(await authApi.register({ displayName, email, password })),
    logout: async () => {
      try { await authApi.logout(); } finally { endSession(); }
    },
    retry: checkSession,
  }), [status, user, accept, endSession, checkSession]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
