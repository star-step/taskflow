import { createContext, useContext, type ReactNode } from 'react';

export interface ToastApi {
  success: (message: ReactNode) => void;
  error(message: ReactNode, action?: { label: string; run: () => void }): void;
}

export const ToastContext = createContext<ToastApi | null>(null);

export function useToast(): ToastApi {
  const ctx = useContext(ToastContext);
  if (!ctx) throw new Error('useToast must be used inside <Toaster>');
  return ctx;
}
