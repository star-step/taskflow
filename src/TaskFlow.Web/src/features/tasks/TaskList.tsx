import type { Task, TaskStatus } from '../../api/types';
import { Assignee, PriorityBadge } from '../../components/Badges';
import { priorityClass } from '../../components/badgeClasses';
import { EditIcon, MoreIcon, TrashIcon } from '../../components/Icons';
import { Menu } from '../../components/Menu';
import { StatusSelect } from '../../components/StatusSelect';
import ui from '../../components/ui.module.css';
import { relativeTime } from '../../hooks/format';
import styles from './tasks.module.css';

interface Props {
  tasks: Task[];
  selectedId: number | null;
  onOpen: (task: Task) => void;
  onStatus: (task: Task, status: TaskStatus) => void;
  onDelete: (task: Task) => void;
}

/**
 * A row opens the task, except when the click came from a control inside it (the status dropdown,
 * the actions menu, the title button). React bubbles portal events (menus, listboxes) through the
 * row too, so those are recognised by role.
 */
function fromControl(target: EventTarget): boolean {
  return target instanceof Element
    && !!target.closest('button, a, input, select, textarea, [role="listbox"], [role="option"], [role="menu"], [role="menuitem"]');
}

const stripe = { Critical: 'var(--crit)', High: 'var(--high)', Medium: 'var(--med)', Low: 'var(--low)' } as const;

function RowMenu({ task, onOpen, onDelete }: { task: Task; onOpen: (t: Task) => void; onDelete: (t: Task) => void }) {
  return (
    <Menu
      trigger={<button type="button" className={ui.iconBtn} aria-label={`Actions for task #${task.id}`} onClick={(e) => e.stopPropagation()}><MoreIcon /></button>}
      items={[
        { label: 'Edit', icon: <EditIcon />, onSelect: () => onOpen(task) },
        { label: 'Delete', icon: <TrashIcon />, danger: true, onSelect: () => onDelete(task) },
      ]}
    />
  );
}

/** Desktop/tablet table and phone card list. CSS shows one or the other by width. */
export function TaskList({ tasks, selectedId, onOpen, onStatus, onDelete }: Props) {
  return (
    <>
      <div className={styles.tableWrap}>
        <table className={styles.table}>
          <thead>
            <tr>
              <th className={styles.colId} scope="col">ID</th>
              <th scope="col">Task</th>
              <th scope="col">Status</th>
              <th scope="col">Priority</th>
              <th scope="col">Assigned to</th>
              <th className={styles.colMod} scope="col">Modified</th>
              <th className={styles.colAct} scope="col"><span className="visually-hidden">Actions</span></th>
            </tr>
          </thead>
          <tbody>
            {tasks.map((t) => (
              <tr key={t.id} className={`${t.priority === 'Critical' ? styles.isCrit : ''} ${t.id === selectedId ? styles.isSelected : ''}`}
                onClick={(e) => { if (!fromControl(e.target)) onOpen(t); }}>
                <td className={styles.colId}><span className={styles.id}>#{t.id}</span></td>
                <td className={styles.titleCell}>
                  <button type="button" className={styles.titleBtn} onClick={() => onOpen(t)}>{t.title}</button>
                  {t.description && <div className={styles.desc}>{t.description}</div>}
                </td>
                <td><StatusSelect taskId={t.id} value={t.status} onChange={(s) => onStatus(t, s)} /></td>
                <td><PriorityBadge priority={t.priority} /></td>
                <td><Assignee name={t.assignedTo} /></td>
                <td className={styles.colMod}><time dateTime={t.modifiedDate} title={new Date(t.modifiedDate).toLocaleString()}>{relativeTime(t.modifiedDate)}</time></td>
                <td className={styles.colAct}><RowMenu task={t} onOpen={onOpen} onDelete={onDelete} /></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <ul className={styles.cards}>
        {tasks.map((t) => (
          <li key={t.id}>
            <article className={styles.card} style={{ ['--stripe' as string]: stripe[t.priority] }} aria-labelledby={`card-${t.id}`}>
              <div className={styles.cardTop}>
                <h3 className={styles.cardTitle} id={`card-${t.id}`}>
                  <button type="button" className={styles.titleBtn} onClick={() => onOpen(t)}>{t.title}</button>
                </h3>
                <RowMenu task={t} onOpen={onOpen} onDelete={onDelete} />
              </div>
              <div className={styles.cardRow}>
                <StatusSelect taskId={t.id} value={t.status} onChange={(s) => onStatus(t, s)} size="lg" />
                <span className={priorityClass[t.priority]}><PriorityBadge priority={t.priority} /></span>
              </div>
              <div className={styles.cardFoot}>
                <Assignee name={t.assignedTo} />
                <span><span className={styles.id}>#{t.id}</span> · {relativeTime(t.modifiedDate)}</span>
              </div>
            </article>
          </li>
        ))}
      </ul>
    </>
  );
}

export function TaskListSkeleton() {
  return (
    <div className={styles.tableWrap} aria-busy="true" aria-label="Loading tasks">
      {Array.from({ length: 6 }, (_, i) => (
        <div key={i} className={styles.skelRow}>
          <span className={styles.skel} style={{ width: '40%' }} />
          <span className={styles.skel} style={{ width: 90 }} />
          <span className={styles.skel} style={{ width: 70 }} />
        </div>
      ))}
    </div>
  );
}
