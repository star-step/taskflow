import { useCallback, useMemo } from 'react';
import { useSearchParams } from 'react-router';
import { PRIORITIES, STATUSES, type SortBy, type SortDir, type TaskFilters, type TaskPriority, type TaskStatus } from '../api/types';

const SORTS: SortBy[] = ['Priority', 'Modified', 'Created', 'Title', 'Status'];
export type View = 'list' | 'board';

/**
 * Filters, sort and view live in the URL (?status=ToDo&priority=High&q=…&view=board), so a view
 * can be bookmarked or shared and survives a refresh.
 */
export function useTaskFilters() {
  const [params, setParams] = useSearchParams();

  const filters = useMemo<TaskFilters>(() => {
    const sortBy = params.get('sort') as SortBy | null;
    return {
      status: params.getAll('status').filter((s): s is TaskStatus => STATUSES.includes(s as TaskStatus)),
      priority: params.getAll('priority').filter((p): p is TaskPriority => PRIORITIES.includes(p as TaskPriority)),
      assignedTo: params.get('assignee') ?? '',
      search: params.get('q') ?? '',
      sortBy: sortBy && SORTS.includes(sortBy) ? sortBy : 'Priority',
      sortDir: (params.get('dir') === 'Asc' ? 'Asc' : 'Desc') as SortDir,
    };
  }, [params]);

  const view: View = params.get('view') === 'board' ? 'board' : 'list';

  const update = useCallback((change: (p: URLSearchParams) => void) => {
    setParams((prev) => {
      const next = new URLSearchParams(prev);
      change(next);
      return next;
    }, { replace: true });
  }, [setParams]);

  const setList = (key: string, values: string[]) => update((p) => {
    p.delete(key);
    values.forEach((v) => p.append(key, v));
  });

  const setValue = (key: string, value: string, fallback = '') => update((p) => {
    if (value && value !== fallback) p.set(key, value); else p.delete(key);
  });

  return {
    filters,
    view,
    activeCount: filters.status.length + filters.priority.length + (filters.assignedTo ? 1 : 0),
    setStatus: (v: TaskStatus[]) => setList('status', v),
    setPriority: (v: TaskPriority[]) => setList('priority', v),
    setAssignee: (v: string) => setValue('assignee', v),
    setSearch: (v: string) => setValue('q', v),
    setSort: (sortBy: SortBy, sortDir: SortDir) => update((p) => {
      if (sortBy === 'Priority') p.delete('sort'); else p.set('sort', sortBy);
      if (sortDir === 'Desc') p.delete('dir'); else p.set('dir', sortDir);
    }),
    setView: (v: View) => setValue('view', v, 'list'),
    clear: () => update((p) => ['status', 'priority', 'assignee', 'q'].forEach((k) => p.delete(k))),
  };
}
