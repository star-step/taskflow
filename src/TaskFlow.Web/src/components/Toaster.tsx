import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react';
import { AlertIcon, CheckIcon, CloseIcon } from './Icons';
import styles from './Toaster.module.css';

interface Toast {
  id: number;
  kind: 'success' | 'error';
  message: ReactNode;
  action?: { label: string; run: () => void };
}

interface ToastApi {
  success(message: ReactNode): void;
  error(message: ReactNode, action?: Toast['action']): void;
}

const ToastContext = createContext<ToastApi | null>(null);
let nextId = 1;

export function Toaster({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);

  const dismiss = useCallback((id: number) => setToasts((all) => all.filter((t) => t.id !== id)), []);

  const push = useCallback((toast: Omit<Toast, 'id'>) => {
    const id = nextId++;
    setToasts((all) => [...all.slice(-2), { ...toast, id }]);
    setTimeout(() => dismiss(id), toast.kind === 'error' ? 8000 : 4000);
  }, [dismiss]);

  const api = useMemo<ToastApi>(() => ({
    success: (message) => push({ kind: 'success', message }),
    error: (message, action) => push({ kind: 'error', message, action }),
  }), [push]);

  return (
    <ToastContext.Provider value={api}>
      {children}
      <div className={styles.region} role="status" aria-live="polite">
        {toasts.map((t) => (
          <div key={t.id} className={styles.toast}>
            <span className={t.kind === 'error' ? styles.errorIcon : styles.okIcon}>
              {t.kind === 'error' ? <AlertIcon /> : <CheckIcon />}
            </span>
            <span className={styles.message}>{t.message}</span>
            {t.action && (
              <button type="button" className={styles.action} onClick={() => { t.action!.run(); dismiss(t.id); }}>
                {t.action.label}
              </button>
            )}
            <button type="button" className={styles.close} onClick={() => dismiss(t.id)} aria-label="Dismiss">
              <CloseIcon />
            </button>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}

export function useToast(): ToastApi {
  const ctx = useContext(ToastContext);
  if (!ctx) throw new Error('useToast must be used inside <Toaster>');
  return ctx;
}
