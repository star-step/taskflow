import { PRIORITY_LEVEL, STATUS_LABEL, type TaskPriority, type TaskStatus } from '../api/types';
import { avatarColour, initials } from '../hooks/format';
import styles from './Badges.module.css';
import { priorityClass, statusClass } from './badgeClasses';

/** Signal bars: the count carries the priority as well as the colour, for colour-blind users. */
export function PriorityBars({ priority }: { priority: TaskPriority }) {
  const level = PRIORITY_LEVEL[priority];
  return (
    <svg className={styles.bars} width="14" height="14" viewBox="0 0 14 14" aria-hidden focusable={false}>
      {[0, 1, 2, 3].map((i) => (
        <rect key={i} x={i * 3.5 + 0.5} y={11 - i * 3} width="2.5" height={3 + i * 3} rx="0.8"
          fill="currentColor" className={i < level ? undefined : styles.off} />
      ))}
    </svg>
  );
}

/** onDark uses lighter shades that stay readable on a dark background. */
export function PriorityBadge({ priority, onDark }: { priority: TaskPriority; onDark?: boolean }) {
  return (
    <span className={`${styles.priority} ${priorityClass[priority]} ${onDark ? styles.onDark : ''}`}>
      <PriorityBars priority={priority} />
      {priority}
    </span>
  );
}

export function StatusIcon({ status }: { status: TaskStatus }) {
  if (status === 'ToDo') {
    return <svg width="14" height="14" viewBox="0 0 14 14" aria-hidden focusable={false}><circle cx="7" cy="7" r="5.25" fill="none" stroke="currentColor" strokeWidth="1.5" strokeDasharray="2.2 1.8" /></svg>;
  }
  if (status === 'InProgress') {
    return <svg width="14" height="14" viewBox="0 0 14 14" aria-hidden focusable={false}><circle cx="7" cy="7" r="5.25" fill="none" stroke="currentColor" strokeWidth="1.5" /><path d="M7 3.5a3.5 3.5 0 010 7z" fill="currentColor" /></svg>;
  }
  return <svg width="14" height="14" viewBox="0 0 14 14" aria-hidden focusable={false}><circle cx="7" cy="7" r="6" fill="currentColor" /><path d="M4.3 7.2l1.8 1.8 3.6-3.8" fill="none" stroke="#fff" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" /></svg>;
}

export function StatusPill({ status }: { status: TaskStatus }) {
  return (
    <span className={`${styles.pill} ${statusClass[status]}`}>
      <StatusIcon status={status} />
      {STATUS_LABEL[status]}
    </span>
  );
}

export function Avatar({ name, size = 'sm' }: { name: string; size?: 'sm' | 'md' }) {
  return (
    <span className={`${styles.avatar} ${size === 'md' ? styles.md : ''}`} style={{ background: avatarColour(name) }} aria-hidden>
      {initials(name)}
    </span>
  );
}

export function Assignee({ name }: { name: string | null }) {
  if (!name) return <span className={styles.unassigned}>Unassigned</span>;
  return <span className={styles.who}><Avatar name={name} />{name}</span>;
}
