import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { authApi } from '../api/auth';
import { configureAuth } from '../api/client';
import type { AuthResponse, User } from '../api/types';
import { AuthContext, type AuthContextValue, type AuthStatus } from './authContext';

/** Refresh this long before the access token expires, so requests rarely hit a 401. */
const REFRESH_EARLY_MS = 60_000;

/**
 * Holds the access token in memory only (never localStorage). On start-up it asks the API to
 * restore the session from the httpOnly refresh cookie; afterwards it refreshes shortly before
 * the token expires. Concurrent refreshes share one request.
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const [status, setStatus] = useState<AuthStatus>('loading');
  const [user, setUser] = useState<User | null>(null);
  const tokenRef = useRef<string | null>(null);
  const refreshing = useRef<Promise<string | null> | null>(null);
  const timer = useRef<ReturnType<typeof setTimeout>>();

  const endSession = useCallback(() => {
    clearTimeout(timer.current);
    tokenRef.current = null;
    setUser(null);
    setStatus('anonymous');
    queryClient.clear();
  }, [queryClient]);

  // accept() schedules the next refresh, and refresh() calls accept(). The ref breaks that cycle
  // so both callbacks keep honest dependency lists.
  const refreshRef = useRef<() => Promise<string | null>>(async () => null);

  const accept = useCallback((res: AuthResponse) => {
    tokenRef.current = res.accessToken;
    setUser(res.user);
    setStatus('authenticated');
    clearTimeout(timer.current);
    const wait = new Date(res.expiresAt).getTime() - Date.now() - REFRESH_EARLY_MS;
    timer.current = setTimeout(() => {
      void refreshRef.current().then((t) => { if (!t) endSession(); });
    }, Math.max(wait, 5_000));
  }, [endSession]);

  const refresh = useCallback((): Promise<string | null> => {
    refreshing.current ??= authApi
      .refresh()
      .then((res) => {
        accept(res);
        return res.accessToken;
      })
      .catch(() => null)
      .finally(() => {
        refreshing.current = null;
      });
    return refreshing.current;
  }, [accept]);

  useEffect(() => { refreshRef.current = refresh; }, [refresh]);

  useEffect(() => {
    configureAuth({ getToken: () => tokenRef.current, refresh, onSessionEnded: endSession });
    void refresh().then((t) => { if (!t) setStatus('anonymous'); });
    return () => clearTimeout(timer.current);
  }, [refresh, endSession]);

  const value = useMemo<AuthContextValue>(() => ({
    status,
    user,
    login: async (email, password) => accept(await authApi.login({ email, password })),
    register: async (displayName, email, password) => accept(await authApi.register({ displayName, email, password })),
    logout: async () => {
      try { await authApi.logout(); } finally { endSession(); }
    },
  }), [status, user, accept, endSession]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
