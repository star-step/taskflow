import { PRIORITIES, STATUSES, STATUS_LABEL, type SortBy, type SortDir, type TaskFilters, type TaskPriority, type TaskStatus } from '../../api/types';
import { PriorityBars } from '../../components/Badges';
import { priorityClass } from '../../components/badgeClasses';
import { Sheet } from '../../components/Overlay';
import { Select } from '../../components/Select';
import ui from '../../components/ui.module.css';
import { SORT_OPTIONS, assigneeOptions, toggle } from './filterOptions';
import styles from './tasks.module.css';

/** Filters for tablet and phone. Changes apply immediately; the button shows how many tasks match. */
export function FilterSheet({ open, onOpenChange, filters, assignees, resultCount, onStatus, onPriority, onAssignee, onSort, onClear }: {
  open: boolean; onOpenChange: (v: boolean) => void; filters: TaskFilters; assignees: string[]; resultCount: number | undefined;
  onStatus: (v: TaskStatus[]) => void; onPriority: (v: TaskPriority[]) => void; onAssignee: (v: string) => void;
  onSort: (by: SortBy, dir: SortDir) => void; onClear: () => void;
}) {
  const sortValue = `${filters.sortBy}:${filters.sortDir}`;
  return (
    <Sheet open={open} onOpenChange={onOpenChange} title="Filters"
      headerAction={<button type="button" className={ui.link} onClick={onClear}>Reset</button>}
      footer={
        <button type="button" className={`${ui.btn} ${ui.primary} ${ui.block}`} onClick={() => onOpenChange(false)}>
          {resultCount === undefined ? 'Show tasks' : `Show ${resultCount} task${resultCount === 1 ? '' : 's'}`}
        </button>
      }>
      <section className={styles.sheetGroup}>
        <h3>Status</h3>
        <div className={styles.chips}>
          {STATUSES.map((s) => (
            <button key={s} type="button" className={ui.chip} aria-pressed={filters.status.includes(s)}
              onClick={() => onStatus(toggle(filters.status, s))}>{STATUS_LABEL[s]}</button>
          ))}
        </div>
      </section>
      <section className={styles.sheetGroup}>
        <h3>Priority</h3>
        <div className={styles.chips}>
          {PRIORITIES.map((p) => (
            <button key={p} type="button" className={`${ui.chip} ${styles.prioChip}`} aria-pressed={filters.priority.includes(p)}
              onClick={() => onPriority(toggle(filters.priority, p))}>
              <span className={priorityClass[p]}><PriorityBars priority={p} /></span>{p}
            </button>
          ))}
        </div>
      </section>
      <section className={styles.sheetGroup}>
        <h3 id="sheet-assignee">Assigned to</h3>
        <Select variant="field" label="Assigned to" value={filters.assignedTo || '__any'}
          onChange={(v) => onAssignee(v === '__any' ? '' : v)} options={assigneeOptions(assignees)} />
      </section>
      <section className={styles.sheetGroup}>
        <h3>Sort by</h3>
        <div className={styles.radioList} role="radiogroup" aria-label="Sort by">
          {SORT_OPTIONS.map((o) => (
            <label key={o.value}>
              {o.label}
              <input type="radio" name="sort" checked={sortValue === o.value}
                onChange={() => { const [by, dir] = o.value.split(':') as [SortBy, SortDir]; onSort(by, dir); }} />
            </label>
          ))}
        </div>
      </section>
    </Sheet>
  );
}
