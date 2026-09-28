import { api, CLIENT_HEADER } from './client';
import type { AuthResponse, User } from './types';

export const authApi = {
  register: (body: { displayName: string; email: string; password: string }) =>
    api<AuthResponse>('/auth/register', { method: 'POST', json: body, authenticated: false }),

  login: (body: { email: string; password: string }) =>
    api<AuthResponse>('/auth/login', { method: 'POST', json: body, authenticated: false }),

  refresh: () =>
    api<AuthResponse>('/auth/refresh', { method: 'POST', headers: CLIENT_HEADER, authenticated: false }),

  logout: () =>
    api<void>('/auth/logout', { method: 'POST', headers: CLIENT_HEADER, authenticated: false }),

  me: () => api<User>('/auth/me'),
};
