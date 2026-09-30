import { useRef, type ReactNode } from 'react';
import * as Dialog from '@radix-ui/react-dialog';
import { CloseIcon, TrashIcon } from './Icons';
import ui from './ui.module.css';
import styles from './Overlay.module.css';

/**
 * Overlays are Radix Dialogs, which give every one of them the same behaviour: the page behind is
 * scroll-locked (including iOS), focus is trapped inside and returns to the opener, and Esc closes.
 * Only the overlay's body scrolls, and scrolling never chains to the page.
 */
interface BaseProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: ReactNode;
  children: ReactNode;
  footer?: ReactNode;
}

/**
 * Dialogs move focus inside when they open (Radix default: the first focusable element, the close
 * button). A field marked data-initial-focus gets it instead, e.g. the title of a new task.
 */
function focusMarkedField(event: Event) {
  const field = (event.currentTarget as HTMLElement | null)?.querySelector<HTMLElement>('[data-initial-focus]');
  if (field) {
    event.preventDefault();
    field.focus();
  }
}

/** Centred dialog on desktop; full-screen sheet with pinned header and footer on phones. */
export function Modal({ open, onOpenChange, title, children, footer }: BaseProps) {
  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className={styles.scrim} />
        <Dialog.Content className={styles.modal} aria-describedby={undefined} onOpenAutoFocus={focusMarkedField}>
          <header className={styles.head}>
            <Dialog.Title className={styles.title}>{title}</Dialog.Title>
            <Dialog.Close className={ui.iconBtn} aria-label="Close"><CloseIcon /></Dialog.Close>
          </header>
          <div className={styles.body}>{children}</div>
          {footer && <footer className={styles.foot}>{footer}</footer>}
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}

/** Right-hand drawer on desktop and tablet; full screen on phones. */
export function Drawer({ open, onOpenChange, title, children, footer, headerExtra }: BaseProps & { headerExtra?: ReactNode }) {
  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className={`${styles.scrim} ${styles.light}`} />
        <Dialog.Content className={styles.drawer} aria-describedby={undefined}>
          <header className={styles.drawerHead}>
            <Dialog.Title className={styles.drawerTitle}>{title}</Dialog.Title>
            {headerExtra}
            <span className={styles.spacer} />
            <Dialog.Close className={ui.iconBtn} aria-label="Close"><CloseIcon /></Dialog.Close>
          </header>
          <div className={styles.body}>{children}</div>
          {footer && <footer className={`${styles.foot} ${styles.drawerFoot}`}>{footer}</footer>}
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}

/** Bottom sheet, used for filters on smaller screens. */
export function Sheet({ open, onOpenChange, title, children, footer, headerAction }: BaseProps & { headerAction?: ReactNode }) {
  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className={styles.scrim} />
        <Dialog.Content className={styles.sheet} aria-describedby={undefined}>
          <div className={styles.grab} aria-hidden />
          <header className={styles.sheetHead}>
            <Dialog.Title className={styles.title}>{title}</Dialog.Title>
            {headerAction}
          </header>
          <div className={styles.sheetBody}>{children}</div>
          {footer && <div className={styles.sheetFoot}>{footer}</div>}
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}

/** Confirmation for destructive actions. Focus starts on Cancel so a stray Enter is harmless. */
export function ConfirmDialog({ open, onOpenChange, title, children, confirmLabel, onConfirm, busy }: {
  open: boolean; onOpenChange: (open: boolean) => void; title: string; children: ReactNode;
  confirmLabel: string; onConfirm: () => void; busy?: boolean;
}) {
  const cancelRef = useRef<HTMLButtonElement>(null);
  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className={styles.scrim} />
        <Dialog.Content role="alertdialog" className={styles.confirm}
          onOpenAutoFocus={(e) => { e.preventDefault(); cancelRef.current?.focus(); }}>
          <div className={styles.confirmBody}>
            <div className={styles.confirmIcon}><TrashIcon /></div>
            <Dialog.Title className={styles.title}>{title}</Dialog.Title>
            <Dialog.Description className={styles.confirmText}>{children}</Dialog.Description>
          </div>
          <footer className={`${styles.foot} ${styles.confirmFoot}`}>
            <Dialog.Close ref={cancelRef} className={ui.btn}>Cancel</Dialog.Close>
            <button type="button" className={`${ui.btn} ${ui.danger}`} onClick={onConfirm} disabled={busy}>
              {busy ? 'Deleting…' : confirmLabel}
            </button>
          </footer>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
