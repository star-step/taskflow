/** Error returned by the API as RFC 7807 ProblemDetails. */
export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly errorCode: string | undefined,
    message: string,
    readonly errors: Record<string, string[]> = {},
    readonly retryAfterSeconds?: number,
  ) {
    super(message);
    this.name = 'ApiError';
  }

  /** First message for a field, e.g. fieldError('title'). */
  fieldError(field: string): string | undefined {
    return this.errors[field]?.[0];
  }
}

interface AuthHooks {
  getToken(): string | null;
  /** Gets a new access token from the refresh cookie; resolves null when the session is over. */
  refresh(): Promise<string | null>;
  onSessionEnded(): void;
}

let auth: AuthHooks | null = null;

export function configureAuth(hooks: AuthHooks): void {
  auth = hooks;
}

export const CLIENT_HEADER = { 'X-TaskFlow-Client': 'web' } as const;

interface RequestOptions extends Omit<RequestInit, 'body'> {
  json?: unknown;
  /** Send the bearer token and retry once after refreshing on 401. Default true. */
  authenticated?: boolean;
}

export async function api<T>(path: string, options: RequestOptions = {}, isRetry = false): Promise<T> {
  const { json, authenticated = true, headers, ...init } = options;
  const finalHeaders = new Headers(headers);
  finalHeaders.set('Accept', 'application/json');
  if (json !== undefined) finalHeaders.set('Content-Type', 'application/json');
  const token = authenticated ? auth?.getToken() : null;
  if (token) finalHeaders.set('Authorization', `Bearer ${token}`);

  let response: Response;
  try {
    response = await fetch(`/api${path}`, {
      ...init,
      credentials: 'same-origin',
      headers: finalHeaders,
      body: json === undefined ? undefined : JSON.stringify(json),
    });
  } catch {
    throw new ApiError(0, 'NETWORK_ERROR', "Couldn't reach the server. Check your connection and try again.");
  }

  if (response.status === 401 && authenticated && auth && !isRetry) {
    const fresh = await auth.refresh();
    if (fresh) return api<T>(path, options, true);
    auth.onSessionEnded();
  }

  if (!response.ok) throw await toApiError(response);
  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

async function toApiError(response: Response): Promise<ApiError> {
  const retryAfter = Number(response.headers.get('Retry-After')) || undefined;
  try {
    const body = await response.json();
    return new ApiError(
      response.status,
      body.errorCode,
      body.detail ?? body.title ?? `Request failed (${response.status}).`,
      body.errors ?? {},
      retryAfter,
    );
  } catch {
    return new ApiError(response.status, undefined, `Request failed (${response.status}).`, {}, retryAfter);
  }
}
