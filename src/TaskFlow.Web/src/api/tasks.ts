import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from './client';
import type { Task, TaskFilters, TaskInput, TaskStatus, TaskSummary, UserSummary } from './types';

export const taskKeys = {
  all: ['tasks'] as const,
  list: (filters: TaskFilters) => ['tasks', 'list', filters] as const,
  summary: ['summary'] as const,
  users: ['users'] as const,
};

function toQueryString(f: TaskFilters): string {
  const p = new URLSearchParams();
  f.status.forEach((s) => p.append('status', s));
  f.priority.forEach((x) => p.append('priority', x));
  if (f.assignedTo) p.set('assignedTo', f.assignedTo);
  if (f.search.trim()) p.set('search', f.search.trim());
  p.set('sortBy', f.sortBy);
  p.set('sortDir', f.sortDir);
  return p.toString();
}

export function useTasks(filters: TaskFilters) {
  return useQuery({
    queryKey: taskKeys.list(filters),
    queryFn: ({ signal }) => api<Task[]>(`/tasks?${toQueryString(filters)}`, { signal }),
    placeholderData: keepPreviousData,
  });
}

export function useSummary() {
  return useQuery({
    queryKey: taskKeys.summary,
    queryFn: ({ signal }) => api<TaskSummary>('/tasks/summary', { signal }),
  });
}

export function useUsers() {
  return useQuery({
    queryKey: taskKeys.users,
    queryFn: ({ signal }) => api<UserSummary[]>('/users', { signal }),
    staleTime: 5 * 60_000,
  });
}

function useInvalidateTasks() {
  const qc = useQueryClient();
  return () => Promise.all([
    qc.invalidateQueries({ queryKey: taskKeys.all }),
    qc.invalidateQueries({ queryKey: taskKeys.summary }),
  ]);
}

export function useCreateTask() {
  const invalidate = useInvalidateTasks();
  return useMutation({
    mutationFn: (input: TaskInput) => api<Task>('/tasks', { method: 'POST', json: input }),
    onSuccess: invalidate,
  });
}

export function useUpdateTask() {
  const invalidate = useInvalidateTasks();
  return useMutation({
    mutationFn: ({ id, input }: { id: number; input: TaskInput }) =>
      api<Task>(`/tasks/${id}`, { method: 'PUT', json: input }),
    onSuccess: invalidate,
  });
}

export function useDeleteTask() {
  const invalidate = useInvalidateTasks();
  return useMutation({
    mutationFn: (id: number) => api<void>(`/tasks/${id}`, { method: 'DELETE' }),
    onSuccess: invalidate,
  });
}

interface StatusChange { id: number; status: TaskStatus }

/**
 * Optimistic status change: every cached task list shows the new status at once. On failure the
 * previous lists are restored and the caller's onError shows what happened.
 */
export function useUpdateStatus(options: { onError?: (change: StatusChange & { previous: TaskStatus }, error: unknown) => void } = {}) {
  const qc = useQueryClient();
  const invalidate = useInvalidateTasks();
  return useMutation({
    mutationFn: ({ id, status }: StatusChange) =>
      api<Task>(`/tasks/${id}/status`, { method: 'PATCH', json: { status } }),
    onMutate: async ({ id, status }) => {
      await qc.cancelQueries({ queryKey: taskKeys.all });
      const snapshot = qc.getQueriesData<Task[]>({ queryKey: taskKeys.all });
      let previous: TaskStatus | undefined;
      qc.setQueriesData<Task[]>({ queryKey: taskKeys.all }, (old) =>
        old?.map((t) => {
          if (t.id !== id) return t;
          previous ??= t.status;
          return { ...t, status };
        }),
      );
      return { snapshot, previous };
    },
    onError: (error, change, context) => {
      context?.snapshot.forEach(([key, data]) => qc.setQueryData(key, data));
      options.onError?.({ ...change, previous: context?.previous ?? change.status }, error);
    },
    onSettled: invalidate,
  });
}
