import type { ReactNode } from 'react';
import * as DropdownMenu from '@radix-ui/react-dropdown-menu';
import styles from './Menu.module.css';

interface MenuItem {
  label: string;
  icon?: ReactNode;
  onSelect: () => void;
  danger?: boolean;
}

/** Small action menu (row ⋯ menu, user menu). */
export function Menu({ trigger, items, header, align = 'end' }: {
  trigger: ReactNode; items: MenuItem[]; header?: ReactNode; align?: 'start' | 'end';
}) {
  return (
    <DropdownMenu.Root modal={false}>
      <DropdownMenu.Trigger asChild>{trigger}</DropdownMenu.Trigger>
      <DropdownMenu.Portal>
        <DropdownMenu.Content className={styles.menu} align={align} sideOffset={6} collisionPadding={8}
          onClick={(e) => e.stopPropagation()}>
          {header && <div className={styles.header}>{header}</div>}
          {items.map((item) => (
            <DropdownMenu.Item key={item.label} className={`${styles.item} ${item.danger ? styles.danger : ''}`}
              onSelect={item.onSelect}>
              {item.icon}
              {item.label}
            </DropdownMenu.Item>
          ))}
        </DropdownMenu.Content>
      </DropdownMenu.Portal>
    </DropdownMenu.Root>
  );
}
