import type { RefObject } from 'react';
import { LIMITS, PRIORITIES, STATUSES, STATUS_LABEL, type SortBy, type SortDir, type TaskFilters, type TaskPriority, type TaskStatus } from '../../api/types';
import { PriorityBars } from '../../components/Badges';
import { priorityClass } from '../../components/badgeClasses';
import { CloseIcon, FilterIcon, SearchIcon } from '../../components/Icons';
import { Select } from '../../components/Select';
import ui from '../../components/ui.module.css';
import { SORT_OPTIONS, assigneeOptions, toggle } from './filterOptions';
import styles from './tasks.module.css';

interface Props {
  filters: TaskFilters;
  assignees: string[];
  activeCount: number;
  searchInputRef: RefObject<HTMLInputElement>;
  search: string;
  onSearch(v: string): void;
  onStatus(v: TaskStatus[]): void;
  onPriority(v: TaskPriority[]): void;
  onAssignee(v: string): void;
  onSort(by: SortBy, dir: SortDir): void;
  onClear(): void;
  onOpenSheet(): void;
}

export function FilterBar({ filters, assignees, activeCount, searchInputRef, search, onSearch, onStatus, onPriority, onAssignee, onSort, onClear, onOpenSheet }: Props) {
  const sortValue = `${filters.sortBy}:${filters.sortDir}`;
  const sortOptions = SORT_OPTIONS.some((o) => o.value === sortValue) ? SORT_OPTIONS : [...SORT_OPTIONS, { value: sortValue, label: sortValue }];
  const setSort = (v: string) => { const [by, dir] = v.split(':') as [SortBy, SortDir]; onSort(by, dir); };

  return (
    <>
      <div className={styles.filters}>
        <div className={styles.fgroup} role="group" aria-label="Filter by status">
          <span className={styles.fgroupLabel} aria-hidden>Status</span>
          {STATUSES.map((s) => (
            <button key={s} type="button" className={ui.chip} aria-pressed={filters.status.includes(s)}
              onClick={() => onStatus(toggle(filters.status, s))}>{STATUS_LABEL[s]}</button>
          ))}
        </div>
        <div className={styles.fgroup} role="group" aria-label="Filter by priority">
          <span className={styles.fgroupLabel} aria-hidden>Priority</span>
          {PRIORITIES.map((p) => (
            <button key={p} type="button" className={`${ui.chip} ${styles.prioChip}`} aria-pressed={filters.priority.includes(p)}
              onClick={() => onPriority(toggle(filters.priority, p))}>
              <span className={priorityClass[p]}><PriorityBars priority={p} /></span>{p}
            </button>
          ))}
        </div>
        {activeCount > 0 && <button type="button" className={ui.link} onClick={onClear}>Clear filters</button>}
        <span className={styles.spacer} />
        <Select label="Assignee" prefix="Assignee:" value={filters.assignedTo || '__any'}
          onChange={(v) => onAssignee(v === '__any' ? '' : v)} options={assigneeOptions(assignees)} />
        <Select label="Sort by" prefix="Sort:" value={sortValue} onChange={setSort}
          options={sortOptions.map((o) => ({ ...o, short: o.label }))} />
      </div>

      <div className={styles.filtersCompact}>
        <label className={`${styles.search} ${styles.inlineSearch}`}>
          <SearchIcon />
          <span className="visually-hidden">Search tasks</span>
          <input ref={searchInputRef} type="search" placeholder="Search tasks" value={search} maxLength={LIMITS.search} onChange={(e) => onSearch(e.target.value)} />
        </label>
        <button type="button" className={ui.btn} onClick={onOpenSheet}>
          <FilterIcon />Filters{activeCount > 0 && <span className={ui.badge}>{activeCount}</span>}
        </button>
        <span className={styles.spacer} />
        <Select label="Sort by" prefix="Sort:" value={sortValue} onChange={setSort} options={sortOptions} />
      </div>

      {activeCount > 0 && (
        <div className={styles.activeFilters} aria-label="Active filters">
          {filters.status.map((s) => (
            <button key={s} type="button" className={ui.chip} aria-pressed="true" onClick={() => onStatus(filters.status.filter((x) => x !== s))}
              aria-label={`Remove filter: ${STATUS_LABEL[s]}`}>{STATUS_LABEL[s]}<CloseIcon /></button>
          ))}
          {filters.priority.map((p) => (
            <button key={p} type="button" className={ui.chip} aria-pressed="true" onClick={() => onPriority(filters.priority.filter((x) => x !== p))}
              aria-label={`Remove filter: ${p}`}>{p}<CloseIcon /></button>
          ))}
          {filters.assignedTo && (
            <button type="button" className={ui.chip} aria-pressed="true" onClick={() => onAssignee('')}
              aria-label={`Remove filter: ${filters.assignedTo}`}>{filters.assignedTo}<CloseIcon /></button>
          )}
        </div>
      )}
    </>
  );
}
