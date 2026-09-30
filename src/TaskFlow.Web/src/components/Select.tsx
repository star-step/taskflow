import { useState, type ReactNode } from 'react';
import * as RSelect from '@radix-ui/react-select';
import * as Dialog from '@radix-ui/react-dialog';
import { MOBILE, useMediaQuery } from '../hooks/useMediaQuery';
import { CheckIcon, ChevronIcon } from './Icons';
import styles from './Select.module.css';

export interface SelectOption<T extends string> {
  value: T;
  label: string;
  /** Shorter text for the trigger, e.g. "Priority" instead of "Priority, high to low". */
  short?: string;
  icon?: ReactNode;
}

interface SelectProps<T extends string> {
  value: T;
  onChange: (value: T) => void;
  options: SelectOption<T>[];
  /** Accessible name, and the title of the mobile sheet. */
  label: string;
  /** Visible text before the value, e.g. "Sort:". */
  prefix?: string;
  variant?: 'field' | 'filter';
  id?: string;
  /** Replaces the default trigger content. */
  renderValue?: (option: SelectOption<T>) => ReactNode;
  triggerClassName?: string;
  /** Accessible name for the trigger, if different from label (e.g. "Status for task #6: To Do"). */
  triggerLabel?: string;
  disabled?: boolean;
}

/**
 * The one dropdown used across the app. At ≥ 768 px it is a Radix Select popover (portal,
 * collision-aware, keyboard + typeahead, listbox semantics). Below that it opens the same options
 * in a bottom sheet with large touch targets. Both lock page scroll while open.
 */
export function Select<T extends string>(props: SelectProps<T>) {
  const isMobile = useMediaQuery(MOBILE);
  return isMobile ? <SheetSelect {...props} /> : <PopoverSelect {...props} />;
}

function TriggerContent<T extends string>({ option, prefix, variant, renderValue }: {
  option: SelectOption<T>; prefix?: string; variant: 'field' | 'filter'; renderValue?: (o: SelectOption<T>) => ReactNode;
}) {
  if (renderValue) return <>{renderValue(option)}</>;
  return (
    <>
      {variant === 'field' && option.icon}
      {prefix && <span className={styles.prefix}>{prefix}</span>}
      <span className={styles.value}>{variant === 'filter' ? option.short ?? option.label : option.label}</span>
      <ChevronIcon className={styles.chevron} />
    </>
  );
}

function triggerClass(variant: 'field' | 'filter', extra?: string) {
  return [styles.trigger, variant === 'field' ? styles.fieldTrigger : styles.filterTrigger, extra].filter(Boolean).join(' ');
}

function PopoverSelect<T extends string>({ value, onChange, options, label, prefix, variant = 'filter', id, renderValue, triggerClassName, triggerLabel, disabled }: SelectProps<T>) {
  const selected = options.find((o) => o.value === value) ?? options[0]!;
  return (
    <RSelect.Root value={value} onValueChange={(v) => onChange(v as T)} disabled={disabled}>
      <RSelect.Trigger id={id} className={triggerClass(variant, triggerClassName)} aria-label={triggerLabel ?? label}>
        <TriggerContent option={selected} prefix={prefix} variant={variant} renderValue={renderValue} />
      </RSelect.Trigger>
      <RSelect.Portal>
        <RSelect.Content className={styles.menu} position="popper" sideOffset={6} collisionPadding={8} align="start">
          <RSelect.Viewport className={styles.viewport}>
            {options.map((o) => (
              <RSelect.Item key={o.value} value={o.value} className={styles.option}>
                {o.icon}
                <RSelect.ItemText>{o.label}</RSelect.ItemText>
                <RSelect.ItemIndicator className={styles.check}><CheckIcon /></RSelect.ItemIndicator>
              </RSelect.Item>
            ))}
          </RSelect.Viewport>
        </RSelect.Content>
      </RSelect.Portal>
    </RSelect.Root>
  );
}

function SheetSelect<T extends string>({ value, onChange, options, label, prefix, variant = 'filter', id, renderValue, triggerClassName, triggerLabel, disabled }: SelectProps<T>) {
  const [open, setOpen] = useState(false);
  const selected = options.find((o) => o.value === value) ?? options[0]!;
  return (
    <Dialog.Root open={open} onOpenChange={setOpen}>
      <Dialog.Trigger asChild>
        <button type="button" id={id} className={triggerClass(variant, triggerClassName)} aria-label={triggerLabel ?? label}
          aria-haspopup="listbox" disabled={disabled}>
          <TriggerContent option={selected} prefix={prefix} variant={variant} renderValue={renderValue} />
        </button>
      </Dialog.Trigger>
      <Dialog.Portal>
        <Dialog.Overlay className={styles.scrim} />
        <Dialog.Content className={styles.sheet} aria-describedby={undefined}>
          <div className={styles.grab} aria-hidden />
          <Dialog.Title className={styles.sheetTitle}>{label}</Dialog.Title>
          <div role="listbox" aria-label={label} className={styles.sheetList}>
            {options.map((o) => (
              <button key={o.value} type="button" role="option" aria-selected={o.value === value}
                className={styles.option} data-state={o.value === value ? 'checked' : 'unchecked'}
                onClick={() => { onChange(o.value); setOpen(false); }}>
                {o.icon}
                <span>{o.label}</span>
                {o.value === value && <span className={styles.check}><CheckIcon /></span>}
              </button>
            ))}
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
