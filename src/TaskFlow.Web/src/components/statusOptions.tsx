import { STATUSES, STATUS_LABEL, type TaskStatus } from '../api/types';
import { StatusIcon } from './Badges';
import { statusTextClass } from './badgeClasses';
import type { SelectOption } from './Select';
import styles from './StatusSelect.module.css';

export const statusOptions: SelectOption<TaskStatus>[] = STATUSES.map((s) => ({
  value: s,
  label: STATUS_LABEL[s],
  icon: <span className={`${styles.icon} ${statusTextClass[s]}`}><StatusIcon status={s} /></span>,
}));
