import type { TaskPriority, TaskStatus } from '../api/types';
import styles from './Badges.module.css';

export const priorityClass: Record<TaskPriority, string> = {
  Critical: styles.crit!, High: styles.high!, Medium: styles.med!, Low: styles.low!,
};

export const statusClass: Record<TaskStatus, string> = {
  ToDo: styles.todo!, InProgress: styles.prog!, Done: styles.done!,
};

export const statusTextClass: Record<TaskStatus, string> = {
  ToDo: styles.todoText!, InProgress: styles.progText!, Done: styles.doneText!,
};
