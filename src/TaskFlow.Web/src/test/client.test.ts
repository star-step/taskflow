import { api, ApiError, configureAuth } from '../api/client';

function json(status: number, body: unknown, headers: Record<string, string> = {}) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json', ...headers } });
}

describe('api client', () => {
  afterEach(() => vi.restoreAllMocks());

  it('refreshes once on 401 and retries with the new token', async () => {
    let token = 'expired';
    const refresh = vi.fn(() => { token = 'fresh'; return Promise.resolve(token); });
    configureAuth({ getToken: () => token, refresh });

    const fetchMock = vi.spyOn(globalThis, 'fetch')
      .mockResolvedValueOnce(json(401, { title: 'Unauthorized' }))
      .mockResolvedValueOnce(json(200, [{ id: 1 }]));

    await expect(api('/tasks')).resolves.toEqual([{ id: 1 }]);
    expect(refresh).toHaveBeenCalledTimes(1);
    const retryHeaders = fetchMock.mock.calls[1]![1]!.headers as Headers;
    expect(retryHeaders.get('Authorization')).toBe('Bearer fresh');
  });

  it('throws the 401 when no new token can be had, leaving the session decision to the auth provider', async () => {
    configureAuth({ getToken: () => 'expired', refresh: () => Promise.resolve(null) });
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(json(401, { title: 'Unauthorized', errorCode: 'UNAUTHENTICATED' }));

    await expect(api('/tasks')).rejects.toMatchObject({ status: 401, errorCode: 'UNAUTHENTICATED' });
  });

  it('turns ProblemDetails into an ApiError with field errors', async () => {
    configureAuth({ getToken: () => 't', refresh: () => Promise.resolve(null) });
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(json(400, {
      title: 'Validation failed', detail: 'One or more fields are invalid.', errorCode: 'VALIDATION_FAILED',
      errors: { title: ['Title is required.'] },
    }));

    const error = (await api('/tasks', { method: 'POST', json: {} }).catch((e: unknown) => e)) as ApiError;
    expect(error).toBeInstanceOf(ApiError);
    expect(error.fieldError('title')).toBe('Title is required.');
  });
});
