import { relativeTime } from '../hooks/format';
import { passwordChecks } from '../pages/passwordRules';
import { safeReturnTo } from '../pages/returnTo';
import { emptyTask, toInput, validateTask } from '../features/tasks/taskForm';

describe('validateTask', () => {
  it('requires a title and a priority, like the API', () => {
    expect(validateTask({ ...emptyTask, title: '   ' })).toEqual({
      title: 'Title is required.',
      priority: 'Priority is required.',
    });
  });

  it('accepts a valid task and trims it for the API', () => {
    const values = { ...emptyTask, title: '  Ship it ', priority: 'High' as const, assignedTo: ' ' };
    expect(validateTask(values)).toEqual({});
    expect(toInput(values)).toEqual({ title: 'Ship it', description: null, status: 'ToDo', priority: 'High', assignedTo: null });
  });

  it('limits the title to 200 characters', () => {
    expect(validateTask({ ...emptyTask, title: 'x'.repeat(201), priority: 'Low' }).title).toBe('Title must be 200 characters or fewer.');
  });
});

describe('passwordChecks', () => {
  it('mirrors the length and personal-info rules', () => {
    expect(passwordChecks('short', 'jordan.ellis@company.com', 'Jordan Ellis')).toEqual({ length: false, personal: true });
    expect(passwordChecks('jordan-rocks-2026', 'jordan.ellis@company.com', 'Jordan Ellis')).toEqual({ length: true, personal: false });
    expect(passwordChecks('harbour-lantern-47', 'jordan.ellis@company.com', 'Jordan Ellis')).toEqual({ length: true, personal: true });
  });
});

describe('safeReturnTo', () => {
  it('only allows paths inside the app', () => {
    expect(safeReturnTo('/?view=board')).toBe('/?view=board');
    expect(safeReturnTo('https://evil.example')).toBe('/');
    expect(safeReturnTo('//evil.example')).toBe('/');
    expect(safeReturnTo(null)).toBe('/');
  });
});

describe('relativeTime', () => {
  const now = Date.parse('2026-09-28T12:00:00Z');
  it.each([
    ['2026-09-28T11:59:30Z', 'just now'],
    ['2026-09-28T11:25:00Z', '35m ago'],
    ['2026-09-28T10:00:00Z', '2h ago'],
    ['2026-09-25T12:00:00Z', '3d ago'],
    ['2026-09-14T12:00:00Z', '2w ago'],
  ])('%s → %s', (iso, expected) => expect(relativeTime(iso, now)).toBe(expected));
});
