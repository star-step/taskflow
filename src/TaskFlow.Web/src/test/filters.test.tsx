import type { ReactNode } from 'react';
import { renderHook } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { LIMITS } from '../api/types';
import { useTaskFilters } from '../hooks/useTaskFilters';

const at = (url: string) => ({ children }: { children: ReactNode }) => <MemoryRouter initialEntries={[url]}>{children}</MemoryRouter>;

describe('useTaskFilters', () => {
  it('clamps search and assignee from the URL to the API limits, so an over-long link still loads', () => {
    const { result } = renderHook(() => useTaskFilters(), {
      wrapper: at(`/?q=${'x'.repeat(250)}&assignee=${'y'.repeat(150)}`),
    });

    expect(result.current.filters.search).toHaveLength(LIMITS.search);
    expect(result.current.filters.assignedTo).toHaveLength(LIMITS.assignedTo);
  });

  it('reads repeated status and priority params and ignores unknown values', () => {
    const { result } = renderHook(() => useTaskFilters(), {
      wrapper: at('/?status=ToDo&status=Bogus&priority=High&sort=Title&dir=Asc&view=board'),
    });

    expect(result.current.filters).toMatchObject({ status: ['ToDo'], priority: ['High'], sortBy: 'Title', sortDir: 'Asc' });
    expect(result.current.view).toBe('board');
  });
});
