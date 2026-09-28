import { useEffect, useMemo, useRef, useState } from 'react';
import { ApiError } from '../../api/client';
import { useDeleteTask, useTasks, useUpdateStatus, useUsers } from '../../api/tasks';
import { STATUS_LABEL, type Task, type TaskStatus } from '../../api/types';
import { EmptyIcon, PlusIcon } from '../../components/Icons';
import { ConfirmDialog } from '../../components/Overlay';
import { useToast } from '../../components/Toaster';
import ui from '../../components/ui.module.css';
import { COMPACT, useMediaQuery } from '../../hooks/useMediaQuery';
import { useTaskFilters } from '../../hooks/useTaskFilters';
import { AppHeader } from './AppHeader';
import { FilterBar } from './FilterBar';
import { FilterSheet } from './FilterSheet';
import { SummaryStrip } from './SummaryStrip';
import { TaskBoard } from './TaskBoard';
import { TaskDetailDrawer } from './TaskDetailDrawer';
import { TaskFormDialog } from './TaskFormDialog';
import { TaskList, TaskListSkeleton } from './TaskList';
import styles from './tasks.module.css';

const SORT_LABEL = { Priority: 'priority', Modified: 'last modified', Created: 'date created', Title: 'title', Status: 'status' } as const;

export function TasksPage() {
  const f = useTaskFilters();
  const toast = useToast();
  const isCompact = useMediaQuery(COMPACT);
  const searchRef = useRef<HTMLInputElement>(null);

  // Search is typed into local state and pushed to the URL (and the API) after a short pause.
  const [search, setSearch] = useState(f.filters.search);
  useEffect(() => { setSearch(f.filters.search); }, [f.filters.search]);
  useEffect(() => {
    const t = setTimeout(() => { if (search !== f.filters.search) f.setSearch(search); }, 250);
    return () => clearTimeout(t);
  }, [search]); // eslint-disable-line react-hooks/exhaustive-deps

  const tasks = useTasks(f.filters);
  const users = useUsers();
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [creating, setCreating] = useState(false);
  const [sheetOpen, setSheetOpen] = useState(false);
  const [deleting, setDeleting] = useState<Task | null>(null);
  const del = useDeleteTask();

  const updateStatus = useUpdateStatus({
    onError: (change, error) => toast.error(
      <>Couldn't move <b>#{change.id}</b> to {STATUS_LABEL[change.status]}, so it's back in {STATUS_LABEL[change.previous]}.
        {error instanceof ApiError && error.status === 404 ? ' It may have been deleted.' : ''}</>,
      { label: 'Retry', run: () => updateStatus.mutate({ id: change.id, status: change.status }) },
    ),
  });

  const list = tasks.data ?? [];
  const selected = list.find((t) => t.id === selectedId) ?? null;
  const assignees = useMemo(() => {
    const names = new Set<string>([...(users.data ?? []).map((u) => u.displayName), ...list.map((t) => t.assignedTo).filter((n): n is string => !!n)]);
    if (f.filters.assignedTo) names.add(f.filters.assignedTo);
    return [...names].sort((a, b) => a.localeCompare(b));
  }, [users.data, list, f.filters.assignedTo]);

  const changeStatus = (task: Task, status: TaskStatus) => { if (task.status !== status) updateStatus.mutate({ id: task.id, status }); };

  async function confirmDelete() {
    if (!deleting) return;
    const target = deleting;
    try {
      await del.mutateAsync(target.id);
      toast.success(<>Deleted <b>#{target.id}</b> {target.title}</>);
      setDeleting(null);
      if (selectedId === target.id) setSelectedId(null);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Couldn't delete the task.");
    }
  }

  const filtered = f.activeCount > 0 || !!f.filters.search;
  const subtitle = tasks.isPending
    ? 'Loading…'
    : filtered ? `${list.length} matching task${list.length === 1 ? '' : 's'}` : `${list.length} task${list.length === 1 ? '' : 's'} · sorted by ${SORT_LABEL[f.filters.sortBy]}`;

  return (
    <>
      <AppHeader search={search} onSearch={setSearch} view={f.view} onView={f.setView} onNewTask={() => setCreating(true)}
        onSearchFocus={() => searchRef.current?.focus()} />
      <main className={styles.page}>
        <div className={styles.pageHead}>
          <div>
            <h1>Team tasks</h1>
            <p aria-live="polite">{subtitle}</p>
          </div>
        </div>

        <SummaryStrip activeStatus={f.filters.status} onStatus={f.setStatus} />

        <FilterBar filters={f.filters} assignees={assignees} activeCount={f.activeCount} searchInputRef={searchRef}
          search={search} onSearch={setSearch} onStatus={f.setStatus} onPriority={f.setPriority} onAssignee={f.setAssignee}
          onSort={f.setSort} onClear={() => { setSearch(''); f.clear(); }} onOpenSheet={() => setSheetOpen(true)} />

        {tasks.isPending ? (
          <TaskListSkeleton />
        ) : tasks.isError ? (
          <div className={styles.empty} role="alert">
            <EmptyIcon />
            <h2>Tasks couldn't be loaded</h2>
            <p>{tasks.error instanceof Error ? tasks.error.message : 'Something went wrong.'}</p>
            <button type="button" className={ui.btn} onClick={() => void tasks.refetch()}>Try again</button>
          </div>
        ) : list.length === 0 ? (
          <div className={styles.empty}>
            <EmptyIcon />
            {filtered ? (
              <>
                <h2>{f.filters.search ? <>No tasks match “{f.filters.search}”</> : 'No tasks match these filters'}</h2>
                <p>Check the spelling, or clear the filters to see everything.</p>
                <button type="button" className={ui.btn} onClick={() => { setSearch(''); f.clear(); }}>Clear filters</button>
              </>
            ) : (
              <>
                <h2>No tasks yet</h2>
                <p>Create the first one for your team.</p>
                <button type="button" className={`${ui.btn} ${ui.primary}`} onClick={() => setCreating(true)}><PlusIcon />New task</button>
              </>
            )}
          </div>
        ) : f.view === 'board' ? (
          <TaskBoard tasks={list} onOpen={(t) => setSelectedId(t.id)} onStatus={changeStatus} />
        ) : (
          <TaskList tasks={list} selectedId={selectedId} onOpen={(t) => setSelectedId(t.id)} onStatus={changeStatus} onDelete={setDeleting} />
        )}
      </main>

      <button type="button" className={styles.fab} aria-label="New task" onClick={() => setCreating(true)}><PlusIcon /></button>

      <TaskFormDialog open={creating} onOpenChange={setCreating} assignees={assignees} />
      <TaskDetailDrawer task={selected} onClose={() => setSelectedId(null)} onDelete={setDeleting} assignees={assignees} />
      {isCompact && (
        <FilterSheet open={sheetOpen} onOpenChange={setSheetOpen} filters={f.filters} assignees={assignees}
          resultCount={tasks.isFetching ? undefined : list.length} onStatus={f.setStatus} onPriority={f.setPriority}
          onAssignee={f.setAssignee} onSort={f.setSort} onClear={() => { setSearch(''); f.clear(); }} />
      )}
      <ConfirmDialog open={!!deleting} onOpenChange={(o) => { if (!o) setDeleting(null); }} title="Delete this task?"
        confirmLabel="Delete task" onConfirm={() => void confirmDelete()} busy={del.isPending}>
        “{deleting?.title}” will be removed from the list and board for everyone. It is kept in the database as a deleted record.
      </ConfirmDialog>
    </>
  );
}
