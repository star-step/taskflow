import { useSummary } from '../../api/tasks';
import { PRIORITIES, STATUSES, STATUS_LABEL, type TaskStatus } from '../../api/types';
import { PriorityBars, StatusIcon } from '../../components/Badges';
import { priorityClass, statusTextClass } from '../../components/badgeClasses';
import ui from '../../components/ui.module.css';
import styles from './tasks.module.css';

const PRIORITY_VAR = { Critical: 'var(--crit)', High: 'var(--high)', Medium: 'var(--med)', Low: 'var(--low)' } as const;

/** Counts from GET /api/tasks/summary (the raw SQL endpoint). Status counts double as filters. */
export function SummaryStrip({ activeStatus, onStatus }: { activeStatus: TaskStatus[]; onStatus: (s: TaskStatus[]) => void }) {
  const { data, isPending, isError, refetch } = useSummary();

  if (isPending) {
    return (
      <div className={styles.summary} aria-busy="true" aria-label="Loading task counts">
        {['All tasks', ...STATUSES.map((s) => STATUS_LABEL[s])].map((label) => (
          <div key={label} className={styles.stat}><span className={styles.statLabel}>{label}</span><span className={styles.skel} style={{ height: 22, width: 40 }} /></div>
        ))}
        <div className={`${styles.stat} ${styles.statPrio}`}><span className={styles.statLabel}>By priority</span><span className={styles.skel} style={{ height: 8 }} /></div>
      </div>
    );
  }

  if (isError) {
    return (
      <div className={styles.summaryError} role="status">
        Task counts are unavailable right now.
        <button type="button" className={ui.link} onClick={() => void refetch()}>Retry</button>
      </div>
    );
  }

  const only = activeStatus.length === 1 ? activeStatus[0] : null;
  return (
    <div className={styles.summary}>
      <button type="button" className={styles.stat} aria-pressed={activeStatus.length === 0} onClick={() => onStatus([])}>
        <span className={styles.statLabel}>All tasks</span>
        <span className={styles.statValue}>{data.total}</span>
      </button>
      {STATUSES.map((s) => (
        <button key={s} type="button" className={styles.stat} aria-pressed={only === s}
          onClick={() => onStatus(only === s ? [] : [s])} aria-label={`${STATUS_LABEL[s]}: ${data.byStatus[s]} tasks. Show only these.`}>
          <span className={`${styles.statLabel} ${statusTextClass[s]}`}><StatusIcon status={s} />{STATUS_LABEL[s]}</span>
          <span className={styles.statValue}>{data.byStatus[s]}</span>
        </button>
      ))}
      <div className={`${styles.stat} ${styles.statPrio}`}>
        <span className={styles.statLabel}>By priority</span>
        <div className={styles.dist} role="img" aria-label={PRIORITIES.map((p) => `${p} ${data.byPriority[p]}`).join(', ')}>
          {PRIORITIES.filter((p) => data.byPriority[p] > 0).map((p) => (
            <span key={p} style={{ flex: data.byPriority[p], background: PRIORITY_VAR[p] }} />
          ))}
        </div>
        <div className={styles.distLegend} aria-hidden>
          {PRIORITIES.map((p) => (
            <span key={p} className={priorityClass[p]}><PriorityBars priority={p} /><span className={styles.muted}>{p}</span><b>{data.byPriority[p]}</b></span>
          ))}
        </div>
      </div>
    </div>
  );
}
