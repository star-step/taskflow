import type { ReactNode } from 'react';
import { act, render, renderHook, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router';
import { AuthProvider } from '../auth/AuthProvider';
import { RequireAuth } from '../auth/RequireAuth';
import { taskKeys, useUpdateStatus } from '../api/tasks';
import type { Task, TaskFilters } from '../api/types';
import { TaskBoard } from '../features/tasks/TaskBoard';
import { LoginPage } from '../pages/LoginPage';
import { RegisterPage } from '../pages/RegisterPage';

type Handler = (init?: RequestInit) => Response | Promise<Response>;

/** Routes fetch calls by "METHOD /api/path" to handlers; anything else is a 404 ProblemDetails. */
function mockApi(routes: Record<string, Handler>) {
  return vi.spyOn(globalThis, 'fetch').mockImplementation(async (input, init) => {
    const url = new URL(String(input), 'http://localhost');
    const key = `${init?.method ?? 'GET'} ${url.pathname}`;
    const handler = routes[key];
    return handler ? handler(init) : json(404, { title: 'Not found', errorCode: 'NOT_FOUND' });
  });
}

function json(status: number, body: unknown) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });
}

const authResponse = {
  accessToken: 'test-access-token',
  expiresAt: new Date(Date.now() + 15 * 60_000).toISOString(),
  user: { id: 1, email: 'alex.morgan@company.com', displayName: 'Alex Morgan' },
};

function Where() {
  const loc = useLocation();
  return <span data-testid="where">{loc.pathname + loc.search}</span>;
}

function renderApp(path: string, ui: ReactNode) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <MemoryRouter initialEntries={[path]}>
      <QueryClientProvider client={client}>
        <AuthProvider>{ui}<Where /></AuthProvider>
      </QueryClientProvider>
    </MemoryRouter>,
  );
}

afterEach(() => vi.restoreAllMocks());

describe('session restore on start-up', () => {
  const routes = (
    <Routes>
      <Route path="/login" element={<p>Sign-in page</p>} />
      <Route path="/" element={<RequireAuth><p>Task list</p></RequireAuth>} />
    </Routes>
  );

  it('shows the app when the refresh cookie is still valid', async () => {
    const api = mockApi({ 'POST /api/auth/refresh': () => json(200, authResponse) });

    renderApp('/', routes);

    expect(await screen.findByText('Task list')).toBeInTheDocument();
    const [, init] = api.mock.calls[0]!;
    expect(new Headers(init!.headers).get('X-TaskFlow-Client')).toBe('web');
  });

  it('sends the user to sign in, keeping where they were going', async () => {
    mockApi({ 'POST /api/auth/refresh': () => json(401, { errorCode: 'INVALID_REFRESH_TOKEN' }) });

    renderApp('/?view=board', routes);

    expect(await screen.findByText('Sign-in page')).toBeInTheDocument();
    expect(screen.getByTestId('where')).toHaveTextContent('/login?returnTo=%2F%3Fview%3Dboard');
  });
});

describe('useUpdateStatus', () => {
  const filters: TaskFilters = { status: [], priority: [], assignedTo: '', search: '', sortBy: 'Priority', sortDir: 'Desc' };
  const task: Task = {
    id: 6, title: 'Add rate limiting', description: null, status: 'ToDo', priority: 'High', assignedTo: null,
    createdDate: '2026-09-22T13:10:00Z', modifiedDate: '2026-09-28T07:00:00Z',
  };

  it('updates the list at once and rolls back when the API refuses', async () => {
    let fail!: () => void;
    mockApi({
      'PATCH /api/tasks/6/status': () => new Promise<Response>((resolve) => {
        fail = () => resolve(json(503, { title: 'Database unavailable', errorCode: 'DATABASE_UNAVAILABLE' }));
      }),
    });
    const client = new QueryClient({ defaultOptions: { queries: { retry: false, staleTime: Infinity } } });
    client.setQueryData(taskKeys.list(filters), [task]);
    const onError = vi.fn();
    const wrapper = ({ children }: { children: ReactNode }) => <QueryClientProvider client={client}>{children}</QueryClientProvider>;
    const { result } = renderHook(() => useUpdateStatus({ onError }), { wrapper });

    act(() => result.current.mutate({ id: 6, status: 'Done' }));

    await waitFor(() => expect(client.getQueryData<Task[]>(taskKeys.list(filters))![0]!.status).toBe('Done'));
    act(() => fail());
    await waitFor(() => expect(onError).toHaveBeenCalledTimes(1));
    expect(onError.mock.calls[0]![0]).toMatchObject({ id: 6, status: 'Done', previous: 'ToDo' });
    expect(client.getQueryData<Task[]>(taskKeys.list(filters))![0]!.status).toBe('ToDo');
  });
});

describe('useUpdateStatus with two changes in flight', () => {
  const filters: TaskFilters = { status: [], priority: [], assignedTo: '', search: '', sortBy: 'Priority', sortDir: 'Desc' };
  const base = { description: null, priority: 'High' as const, assignedTo: null, createdDate: '2026-09-22T13:10:00Z', modifiedDate: '2026-09-28T07:00:00Z' };

  it('rolls back only the task that failed, keeping the other change', async () => {
    let failSix!: () => void;
    mockApi({
      'PATCH /api/tasks/6/status': () => new Promise<Response>((r) => { failSix = () => r(json(503, { errorCode: 'DATABASE_UNAVAILABLE' })); }),
      'PATCH /api/tasks/7/status': () => new Promise<Response>(() => {}), // still pending
    });
    const client = new QueryClient({ defaultOptions: { queries: { retry: false, staleTime: Infinity } } });
    client.setQueryData(taskKeys.list(filters), [
      { ...base, id: 6, title: 'Six', status: 'ToDo' },
      { ...base, id: 7, title: 'Seven', status: 'ToDo' },
    ] satisfies Task[]);
    const wrapper = ({ children }: { children: ReactNode }) => <QueryClientProvider client={client}>{children}</QueryClientProvider>;
    const six = renderHook(() => useUpdateStatus(), { wrapper });
    const seven = renderHook(() => useUpdateStatus(), { wrapper });
    const status = (id: number) => client.getQueryData<Task[]>(taskKeys.list(filters))!.find((t) => t.id === id)!.status;

    act(() => six.result.current.mutate({ id: 6, status: 'Done' }));
    await waitFor(() => expect(status(6)).toBe('Done'));
    act(() => seven.result.current.mutate({ id: 7, status: 'InProgress' }));
    await waitFor(() => expect(status(7)).toBe('InProgress'));

    act(() => failSix());

    await waitFor(() => expect(status(6)).toBe('ToDo'));
    expect(status(7)).toBe('InProgress');
  });
});

describe('TaskBoard column switcher', () => {
  it('is a toggle group, not a half-implemented tablist', () => {
    const tasks: Task[] = [{ id: 1, title: 'One', description: null, status: 'ToDo', priority: 'Low', assignedTo: null,
      createdDate: '2026-09-22T13:10:00Z', modifiedDate: '2026-09-28T07:00:00Z' }];
    render(<TaskBoard tasks={tasks} onOpen={() => {}} onStatus={() => {}} />);

    expect(screen.queryAllByRole('tab')).toHaveLength(0);
    expect(screen.queryAllByRole('tablist')).toHaveLength(0);
    const group = screen.getByRole('group', { name: 'Column' });
    const buttons = within(group).getAllByRole('button');
    expect(buttons).toHaveLength(3);
    expect(buttons[0]).toHaveAttribute('aria-pressed', 'true');
  });
});

describe('LoginPage', () => {
  it('shows the API message and clears the password on a failed sign-in', async () => {
    const message = 'Email or password is incorrect. After several failed attempts, sign-in is paused for 15 minutes.';
    mockApi({
      'POST /api/auth/refresh': () => json(401, {}),
      'POST /api/auth/login': () => json(401, { title: 'Sign-in failed', detail: message, errorCode: 'INVALID_CREDENTIALS' }),
    });
    const user = userEvent.setup();
    renderApp('/login', <LoginPage />);

    await user.type(await screen.findByLabelText('Email'), 'alex.morgan@company.com');
    await user.type(screen.getByLabelText('Password'), 'not-the-password');
    await user.click(screen.getByRole('button', { name: 'Sign in' }));

    expect(await screen.findByRole('alert')).toHaveTextContent(message);
    expect(screen.getByLabelText('Password')).toHaveValue('');
  });

  it('signs in and goes to the return address', async () => {
    mockApi({
      'POST /api/auth/refresh': () => json(401, {}),
      'POST /api/auth/login': () => json(200, authResponse),
    });
    const user = userEvent.setup();
    renderApp('/login?returnTo=%2F%3Fview%3Dboard', <Routes><Route path="/login" element={<LoginPage />} /><Route path="/" element={<p>Home</p>} /></Routes>);

    await user.type(await screen.findByLabelText('Email'), 'alex.morgan@company.com');
    await user.type(screen.getByLabelText('Password'), 'orchard-kettle-2026');
    await user.click(screen.getByRole('button', { name: 'Sign in' }));

    await waitFor(() => expect(screen.getByTestId('where')).toHaveTextContent('/?view=board'));
  });
});

describe('RegisterPage', () => {
  it('validates before calling the API and shows the live password checklist', async () => {
    const api = mockApi({ 'POST /api/auth/refresh': () => json(401, {}) });
    const user = userEvent.setup();
    renderApp('/register', <RegisterPage />);

    await user.click(await screen.findByRole('button', { name: 'Create account' }));
    expect(screen.getByText('Enter your full name.')).toBeInTheDocument();
    expect(screen.getByText('Enter your work email.')).toBeInTheDocument();

    await user.type(screen.getByLabelText('Full name'), 'Jordan Ellis');
    await user.type(screen.getByLabelText('Work email'), 'jordan.ellis@company.com');
    await user.type(screen.getByLabelText('Password'), 'jordan-rocks-2026');
    expect(screen.getByText("Password can't include your name or email.")).toBeInTheDocument();

    await user.clear(screen.getByLabelText('Password'));
    await user.type(screen.getByLabelText('Password'), 'harbour-lantern-47');
    await user.type(screen.getByLabelText('Confirm password'), 'harbour-lantern');
    expect(screen.getByText("Passwords don't match.")).toBeInTheDocument();

    expect(api.mock.calls.some(([url]) => String(url).includes('/auth/register'))).toBe(false);
  });
});
