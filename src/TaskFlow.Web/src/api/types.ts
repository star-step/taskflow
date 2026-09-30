export type TaskStatus = 'ToDo' | 'InProgress' | 'Done';
export type TaskPriority = 'Low' | 'Medium' | 'High' | 'Critical';
export type SortBy = 'Priority' | 'Modified' | 'Created' | 'Title' | 'Status';
export type SortDir = 'Asc' | 'Desc';

export interface Task {
  id: number;
  title: string;
  description: string | null;
  status: TaskStatus;
  priority: TaskPriority;
  assignedTo: string | null;
  createdDate: string;
  modifiedDate: string;
}

export interface TaskInput {
  title: string;
  description: string | null;
  status: TaskStatus;
  priority: TaskPriority;
  assignedTo: string | null;
}

export interface TaskSummary {
  total: number;
  byStatus: Record<TaskStatus, number>;
  byPriority: Record<TaskPriority, number>;
  matrix: { status: TaskStatus; priority: TaskPriority; count: number }[];
}

export interface TaskFilters {
  status: TaskStatus[];
  priority: TaskPriority[];
  assignedTo: string;
  search: string;
  sortBy: SortBy;
  sortDir: SortDir;
}

export interface User {
  id: number;
  email: string;
  displayName: string;
}

export interface UserSummary {
  id: number;
  displayName: string;
}

export interface AuthResponse {
  accessToken: string;
  expiresAt: string;
  user: User;
}

export const STATUSES: TaskStatus[] = ['ToDo', 'InProgress', 'Done'];
export const STATUS_LABEL: Record<TaskStatus, string> = { ToDo: 'To Do', InProgress: 'In Progress', Done: 'Done' };

/** Most urgent first, the order used in pickers and legends. */
export const PRIORITIES: TaskPriority[] = ['Critical', 'High', 'Medium', 'Low'];
export const PRIORITY_LEVEL: Record<TaskPriority, number> = { Low: 1, Medium: 2, High: 3, Critical: 4 };

/** Mirrors TaskFieldLimits and AccountRules in TaskFlow.Shared. */
export const LIMITS = { title: 200, description: 2000, assignedTo: 100, search: 200, passwordMin: 12, passwordMax: 128, displayNameMin: 2, displayNameMax: 100 };
