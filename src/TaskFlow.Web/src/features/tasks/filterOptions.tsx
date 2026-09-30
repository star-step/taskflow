import { Avatar } from '../../components/Badges';
import type { SelectOption } from '../../components/Select';

export const SORT_OPTIONS: SelectOption<string>[] = [
  { value: 'Priority:Desc', label: 'Priority, high to low', short: 'Priority' },
  { value: 'Priority:Asc', label: 'Priority, low to high', short: 'Priority ↑' },
  { value: 'Modified:Desc', label: 'Recently modified', short: 'Modified' },
  { value: 'Created:Desc', label: 'Newest first', short: 'Newest' },
  { value: 'Title:Asc', label: 'Title A–Z', short: 'Title' },
];

export function assigneeOptions(names: string[]): SelectOption<string>[] {
  return [
    { value: '__any', label: 'Anyone' },
    ...names.map((n) => ({ value: n, label: n, icon: <Avatar name={n} /> })),
  ];
}

export function toggle<T>(list: T[], value: T): T[] {
  return list.includes(value) ? list.filter((v) => v !== value) : [...list, value];
}
