import { LIMITS } from '../../api/types';
import { useAuth } from '../../auth/authContext';
import { Avatar } from '../../components/Badges';
import { BoardIcon, ListIcon, PlusIcon, SearchIcon } from '../../components/Icons';
import { Menu } from '../../components/Menu';
import ui from '../../components/ui.module.css';
import type { View } from '../../hooks/useTaskFilters';
import { Brand } from '../../pages/AuthLayout';
import styles from './tasks.module.css';

export function AppHeader({ search, onSearch, view, onView, onNewTask, onSearchFocus }: {
  search: string; onSearch: (v: string) => void; view: View; onView: (v: View) => void; onNewTask: () => void; onSearchFocus: () => void;
}) {
  const { user, logout } = useAuth();
  return (
    <header className={styles.topbar}>
      <Brand />
      <label className={`${styles.search} ${styles.topSearch}`}>
        <SearchIcon />
        <span className="visually-hidden">Search tasks</span>
        <input type="search" placeholder="Search tasks" value={search} maxLength={LIMITS.search} onChange={(e) => onSearch(e.target.value)} />
      </label>
      <span className={styles.spacer} />
      <button type="button" className={`${ui.iconBtn} ${styles.searchIcon}`} aria-label="Search" onClick={onSearchFocus}>
        <SearchIcon />
      </button>
      <div className={styles.seg} role="group" aria-label="View">
        <button type="button" aria-pressed={view === 'list'} onClick={() => onView('list')} aria-label="List view">
          <ListIcon /><span className={styles.segLabel}>List</span>
        </button>
        <button type="button" aria-pressed={view === 'board'} onClick={() => onView('board')} aria-label="Board view">
          <BoardIcon /><span className={styles.segLabel}>Board</span>
        </button>
      </div>
      <button type="button" className={`${ui.btn} ${ui.primary} ${styles.newBtn}`} onClick={onNewTask}>
        <PlusIcon />New task
      </button>
      {user && (
        <Menu
          trigger={<button type="button" className={styles.avatarBtn} aria-label={`Account: ${user.displayName}`}><Avatar name={user.displayName} size="md" /></button>}
          header={<><strong>{user.displayName}</strong><span className={styles.muted}>{user.email}</span></>}
          items={[{ label: 'Sign out', onSelect: () => void logout() }]}
        />
      )}
    </header>
  );
}
