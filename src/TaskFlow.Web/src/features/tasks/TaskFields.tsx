import { useId } from 'react';
import { LIMITS, PRIORITIES } from '../../api/types';
import { PriorityBars } from '../../components/Badges';
import { AlertIcon } from '../../components/Icons';
import { Select } from '../../components/Select';
import { statusOptions } from '../../components/statusOptions';
import ui from '../../components/ui.module.css';
import type { TaskFormErrors, TaskFormValues } from './taskForm';
import styles from './tasks.module.css';

const prioClass = { Critical: styles.pCrit, High: styles.pHigh, Medium: styles.pMed, Low: styles.pLow };

export function TaskFields({ values, errors, onChange, assignees, largeTitle, autoFocus }: {
  values: TaskFormValues; errors: TaskFormErrors; onChange(v: TaskFormValues): void; assignees: string[];
  largeTitle?: boolean; autoFocus?: boolean;
}) {
  const id = useId();
  const set = <K extends keyof TaskFormValues>(key: K, value: TaskFormValues[K]) => onChange({ ...values, [key]: value });
  const err = (key: keyof TaskFormValues) => errors[key] && (
    <span id={`${id}-${key}-error`} className={ui.error}><AlertIcon />{errors[key]}</span>
  );
  const describedBy = (key: keyof TaskFormValues, extra?: string) =>
    [errors[key] ? `${id}-${key}-error` : null, extra].filter(Boolean).join(' ') || undefined;

  return (
    <>
      <div className={ui.field}>
        <label className={ui.label} htmlFor={`${id}-title`}>Title<span className={ui.req} aria-hidden>*</span></label>
        <input id={`${id}-title`} className={`${ui.input} ${largeTitle ? ui.large : ''} ${errors.title ? ui.invalid : ''}`}
          placeholder="What needs doing?" value={values.title} onChange={(e) => set('title', e.target.value)}
          aria-invalid={!!errors.title} aria-describedby={describedBy('title')} aria-required autoFocus={autoFocus} />
        {err('title')}
      </div>

      <div className={ui.field}>
        <label className={ui.label} htmlFor={`${id}-desc`}>Description</label>
        <textarea id={`${id}-desc`} className={`${ui.textarea} ${errors.description ? ui.invalid : ''}`}
          placeholder="Add details, links or acceptance criteria" value={values.description}
          onChange={(e) => set('description', e.target.value)} aria-invalid={!!errors.description}
          aria-describedby={describedBy('description', `${id}-desc-count`)} />
        {err('description') || (
          <span className={ui.hint}><span>Optional</span><span id={`${id}-desc-count`}>{values.description.length} / {LIMITS.description}</span></span>
        )}
      </div>

      <fieldset className={styles.prioField} aria-describedby={describedBy('priority')}>
        <legend className={ui.label}>Priority<span className={ui.req} aria-hidden>*</span></legend>
        <div className={styles.prioPicker}>
          {PRIORITIES.map((p) => (
            <label key={p} className={`${styles.prioOpt} ${prioClass[p]}`}>
              <input type="radio" name={`${id}-priority`} value={p} checked={values.priority === p} onChange={() => set('priority', p)} />
              <PriorityBars priority={p} />{p}
            </label>
          ))}
        </div>
        {err('priority')}
      </fieldset>

      <div className={ui.row2}>
        <div className={ui.field}>
          <label className={ui.label} htmlFor={`${id}-status`}>Status</label>
          <Select id={`${id}-status`} variant="field" label="Status" value={values.status}
            onChange={(v) => set('status', v)} options={statusOptions} />
        </div>
        <div className={ui.field}>
          <label className={ui.label} htmlFor={`${id}-assignee`}>Assigned to</label>
          <input id={`${id}-assignee`} className={`${ui.input} ${errors.assignedTo ? ui.invalid : ''}`} list={`${id}-people`}
            placeholder="Name or email" value={values.assignedTo} onChange={(e) => set('assignedTo', e.target.value)}
            maxLength={LIMITS.assignedTo} aria-invalid={!!errors.assignedTo} aria-describedby={describedBy('assignedTo')} />
          <datalist id={`${id}-people`}>{assignees.map((n) => <option key={n} value={n} />)}</datalist>
          {err('assignedTo')}
        </div>
      </div>
    </>
  );
}
