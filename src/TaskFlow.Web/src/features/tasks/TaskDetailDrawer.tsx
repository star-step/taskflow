import { useEffect, useState, type FormEvent } from 'react';
import { ApiError } from '../../api/client';
import { useUpdateTask } from '../../api/tasks';
import type { Task } from '../../api/types';
import { StatusPill } from '../../components/Badges';
import { TrashIcon } from '../../components/Icons';
import { Drawer } from '../../components/Overlay';
import { useToast } from '../../components/toastContext';
import ui from '../../components/ui.module.css';
import { dateTime } from '../../hooks/format';
import { TaskFields } from './TaskFields';
import { toInput, validateTask, type TaskFormErrors, type TaskFormValues } from './taskForm';
import styles from './tasks.module.css';

const fromTask = (t: Task): TaskFormValues => ({
  title: t.title, description: t.description ?? '', status: t.status, priority: t.priority, assignedTo: t.assignedTo ?? '',
});

/** View and edit one task. Closing with unsaved edits asks first. */
export function TaskDetailDrawer({ task, onClose, onDelete, assignees }: {
  task: Task | null; onClose: () => void; onDelete: (t: Task) => void; assignees: string[];
}) {
  const update = useUpdateTask();
  const toast = useToast();
  const [values, setValues] = useState<TaskFormValues | null>(null);
  const [errors, setErrors] = useState<TaskFormErrors>({});
  const [confirmDiscard, setConfirmDiscard] = useState(false);

  useEffect(() => {
    setValues(task ? fromTask(task) : null);
    setErrors({});
    setConfirmDiscard(false);
  }, [task?.id]); // eslint-disable-line react-hooks/exhaustive-deps -- reset when a different task opens

  const original = task ? fromTask(task) : null;
  const dirty = !!values && !!original && JSON.stringify(values) !== JSON.stringify(original);

  function requestClose(open: boolean) {
    if (open) return;
    if (dirty) setConfirmDiscard(true); else onClose();
  }

  async function save(e?: FormEvent) {
    e?.preventDefault();
    if (!task || !values) return;
    const found = validateTask(values);
    setErrors(found);
    if (Object.keys(found).length) return;
    try {
      await update.mutateAsync({ id: task.id, input: toInput(values) });
      toast.success(<>Saved <b>#{task.id}</b></>);
      onClose();
    } catch (err) {
      if (err instanceof ApiError && Object.keys(err.errors).length) {
        setErrors(Object.fromEntries(Object.entries(err.errors).map(([k, v]) => [k, v[0]])));
      } else {
        toast.error(err instanceof Error ? err.message : "Couldn't save the task.");
      }
    }
  }

  return (
    <Drawer open={!!task} onOpenChange={requestClose} title={task ? `#${task.id}` : ''}
      headerExtra={task && <StatusPill status={task.status} />}
      footer={confirmDiscard ? (
        <div className={styles.discard} role="alert">
          <span>Discard your changes?</span>
          <span className={styles.spacer} />
          <button type="button" className={ui.btn} onClick={() => setConfirmDiscard(false)}>Keep editing</button>
          <button type="button" className={`${ui.btn} ${ui.danger}`} onClick={onClose}>Discard</button>
        </div>
      ) : (
        <>
          <button type="button" className={`${ui.btn} ${ui.ghostDanger}`} onClick={() => task && onDelete(task)}><TrashIcon />Delete</button>
          <span className={styles.spacer} />
          <button type="button" className={ui.btn} onClick={() => requestClose(false)}>Cancel</button>
          <button type="submit" form="edit-task-form" className={`${ui.btn} ${ui.primary}`} disabled={!dirty || update.isPending}>
            {update.isPending ? 'Saving…' : 'Save changes'}
          </button>
        </>
      )}>
      {task && values && (
        <form id="edit-task-form" onSubmit={(e) => void save(e)} noValidate style={{ display: 'contents' }}>
          <TaskFields values={values} errors={errors} onChange={setValues} assignees={assignees} largeTitle />
          <dl className={styles.facts}>
            <dt>Created</dt><dd><time dateTime={task.createdDate}>{dateTime(task.createdDate)}</time></dd>
            <dt>Last modified</dt><dd><time dateTime={task.modifiedDate}>{dateTime(task.modifiedDate)}</time></dd>
          </dl>
        </form>
      )}
    </Drawer>
  );
}
