import { STATUS_LABEL, type TaskStatus } from '../api/types';
import { StatusIcon } from './Badges';
import { ChevronIcon } from './Icons';
import { Select } from './Select';
import { statusOptions } from './statusOptions';
import styles from './StatusSelect.module.css';

const pillColour: Record<TaskStatus, string | undefined> = { ToDo: styles.todo, InProgress: styles.prog, Done: styles.done };

/** The status pill that is also a dropdown. (Rows ignore clicks from controls inside them; see TaskList.) */
export function StatusSelect({ taskId, value, onChange, size = 'md' }: {
  taskId: number; value: TaskStatus; onChange: (value: TaskStatus) => void; size?: 'md' | 'lg';
}) {
  return (
  <Select
      value={value}
      onChange={onChange}
      options={statusOptions}
      label="Set status"
      triggerLabel={`Status for task #${taskId}: ${STATUS_LABEL[value]}`}
      triggerClassName={`${styles.pill} ${pillColour[value]} ${size === 'lg' ? styles.lg : ''}`}
      renderValue={(o) => (
        <>
          <StatusIcon status={o.value} />
          <span>{o.label}</span>
          <ChevronIcon className={styles.chev} />
        </>
      )}
    />
  );
}
