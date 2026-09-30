import { useEffect, useState, type FormEvent } from 'react';
import { ApiError } from '../../api/client';
import { useCreateTask } from '../../api/tasks';
import { Modal } from '../../components/Overlay';
import { useToast } from '../../components/toastContext';
import ui from '../../components/ui.module.css';
import { TaskFields } from './TaskFields';
import { emptyTask, toInput, validateTask, type TaskFormErrors, type TaskFormValues } from './taskForm';

/** Create form. Errors show after the first submit and then update as the user types. */
export function TaskFormDialog({ open, onOpenChange, assignees }: { open: boolean; onOpenChange(v: boolean): void; assignees: string[] }) {
  const create = useCreateTask();
  const toast = useToast();
  const [values, setValues] = useState<TaskFormValues>(emptyTask);
  const [errors, setErrors] = useState<TaskFormErrors>({});
  const [submitted, setSubmitted] = useState(false);

  useEffect(() => {
    if (open) { setValues(emptyTask); setErrors({}); setSubmitted(false); create.reset(); }
    // eslint-disable-next-line react-hooks/exhaustive-deps -- reset only when the dialog opens
  }, [open]);

  const change = (v: TaskFormValues) => { setValues(v); if (submitted) setErrors(validateTask(v)); };

  async function submit(e?: FormEvent) {
    e?.preventDefault();
    setSubmitted(true);
    const found = validateTask(values);
    setErrors(found);
    if (Object.keys(found).length) return;
    try {
      const task = await create.mutateAsync(toInput(values));
      toast.success(<>Created <b>#{task.id}</b> {task.title}</>);
      onOpenChange(false);
    } catch (err) {
      if (err instanceof ApiError && Object.keys(err.errors).length) {
        setErrors(Object.fromEntries(Object.entries(err.errors).map(([k, v]) => [k, v[0]])) as TaskFormErrors);
      } else {
        toast.error(err instanceof Error ? err.message : "Couldn't create the task.");
      }
    }
  }

  return (
    <Modal open={open} onOpenChange={onOpenChange} title="New task"
      footer={<>
        <button type="button" className={ui.btn} onClick={() => onOpenChange(false)}>Cancel</button>
        <button type="submit" form="new-task-form" className={`${ui.btn} ${ui.primary}`} disabled={create.isPending}>
          {create.isPending ? 'Creating…' : 'Create task'}
        </button>
      </>}>
      <form id="new-task-form" onSubmit={submit} noValidate style={{ display: 'contents' }}>
        <TaskFields values={values} errors={errors} onChange={change} assignees={assignees} autoFocus />
      </form>
    </Modal>
  );
}
