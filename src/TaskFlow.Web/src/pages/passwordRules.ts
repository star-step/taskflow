import { LIMITS } from '../api/types';

/** Client-side mirror of the API's password rules. The common-password check stays on the server. */
export function passwordChecks(password: string, email: string, displayName: string) {
  const parts = [
    ...(email.split('@')[0] ?? '').split(/[._+-]/),
    email.split('@')[0] ?? '',
    ...displayName.split(/\s+/),
  ].filter((p) => p.length >= 3);
  const lower = password.toLowerCase();
  return {
    length: password.length >= LIMITS.passwordMin,
    personal: password.length > 0 && !parts.some((p) => lower.includes(p.toLowerCase())),
  };
}
