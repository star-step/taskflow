import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { authApi } from '../api/auth';
import { configureAuth } from '../api/client';
import type { AuthResponse, User } from '../api/types';

type Status = 'loading' | 'authenticated' | 'anonymous';

interface AuthContextValue {
  status: Status;
  user: User | null;
  login(email: string, password: string): Promise<void>;
  register(displayName: string, email: string, password: string): Promise<void>;
  logout(): Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

/** Refresh this long before the access token expires, so requests rarely hit a 401. */
const REFRESH_EARLY_MS = 60_000;

/**
 * Holds the access token in memory only (never localStorage). On start-up it asks the API to
 * restore the session from the httpOnly refresh cookie; afterwards it refreshes shortly before
 * the token expires. Concurrent refreshes share one request.
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const [status, setStatus] = useState<Status>('loading');
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
  }, []);

  const accept = useCallback((res: AuthResponse) => {
    tokenRef.current = res.accessToken;
    setUser(res.user);
    setStatus('authenticated');
    clearTimeout(timer.current);
    const wait = new Date(res.expiresAt).getTime() - Date.now() - REFRESH_EARLY_MS;
    timer.current = setTimeout(() => {
      void refresh().then((t) => { if (!t) endSession(); });
    }, Math.max(wait, 5_000));
  }, [refresh, endSession]);

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

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside <AuthProvider>');
  return ctx;
}
