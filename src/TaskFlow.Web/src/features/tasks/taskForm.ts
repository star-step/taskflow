import { LIMITS, type TaskInput, type TaskPriority, type TaskStatus } from '../../api/types';

export interface TaskFormValues {
  title: string;
  description: string;
  status: TaskStatus;
  priority: TaskPriority | null;
  assignedTo: string;
}

export type TaskFormErrors = Partial<Record<keyof TaskFormValues, string>>;

export const emptyTask: TaskFormValues = { title: '', description: '', status: 'ToDo', priority: null, assignedTo: '' };

/** Same rules as the API's CreateTaskRequest / UpdateTaskRequest. */
export function validateTask(v: TaskFormValues): TaskFormErrors {
  const e: TaskFormErrors = {};
  if (!v.title.trim()) e.title = 'Title is required.';
  else if (v.title.trim().length > LIMITS.title) e.title = `Title must be ${LIMITS.title} characters or fewer.`;
  if (v.description.length > LIMITS.description) e.description = `Description must be ${LIMITS.description} characters or fewer.`;
  if (!v.priority) e.priority = 'Priority is required.';
  if (v.assignedTo.trim().length > LIMITS.assignedTo) e.assignedTo = `Assigned to must be ${LIMITS.assignedTo} characters or fewer.`;
  return e;
}

export function toInput(v: TaskFormValues): TaskInput {
  return {
    title: v.title.trim(),
    description: v.description.trim() || null,
    status: v.status,
    priority: v.priority!,
    assignedTo: v.assignedTo.trim() || null,
  };
}
