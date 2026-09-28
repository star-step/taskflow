import type { FormEvent, ReactNode } from 'react';
import { PriorityBadge, StatusPill } from '../components/Badges';
import { LogoIcon } from '../components/Icons';
import styles from './AuthLayout.module.css';

export function Brand({ inverted }: { inverted?: boolean }) {
  return (
    <span className={`${styles.brand} ${inverted ? styles.inverted : ''}`}>
      <span className={styles.mark}><LogoIcon /></span>TaskFlow
    </span>
  );
}

/** Split layout for sign-in and create-account: a brand panel on desktop, a centred card below that. */
export function AuthLayout({ children, onSubmit }: { children: ReactNode; onSubmit(e: FormEvent<HTMLFormElement>): void }) {
  return (
    <div className={styles.page}>
      <aside className={styles.aside}>
        <Brand inverted />
        <div>
          <h2 className={styles.headline}>Everything the team is working on, in one list.</h2>
          <p className={styles.lead}>See what's urgent, who owns it and what changed, then move work along without a meeting.</p>
        </div>
        <div className={styles.preview} aria-hidden>
          <div className={styles.row}><StatusPill status="InProgress" /><span>Fix date picker timezone offset</span><PriorityBadge priority="Critical" onDark /></div>
          <div className={styles.row}><StatusPill status="ToDo" /><span>Add rate limiting to public endpoints</span><PriorityBadge priority="High" onDark /></div>
          <div className={styles.row}><StatusPill status="InProgress" /><span>Design empty states for dashboard</span><PriorityBadge priority="Medium" onDark /></div>
        </div>
        <p className={styles.foot}>Internal tool · Accounts are limited to company email addresses</p>
      </aside>
      <main className={styles.main}>
        <form className={styles.form} onSubmit={onSubmit} noValidate>
          <span className={styles.mobileBrand}><Brand /></span>
          {children}
        </form>
      </main>
    </div>
  );
}
