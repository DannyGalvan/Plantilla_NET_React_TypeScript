import { act, renderHook } from '@testing-library/react';
import { beforeEach, describe, expect, it } from 'vitest';

import { useSidebarState } from './sidebar-state';

describe('useSidebarState', () => {
  beforeEach(() => localStorage.clear());

  it('starts expanded and toggles between expanded and collapsed', () => {
    const { result } = renderHook(() => useSidebarState());
    expect(result.current.state).toBe('expanded');

    act(() => result.current.toggleCollapsed());
    expect(result.current.state).toBe('collapsed');

    act(() => result.current.toggleCollapsed());
    expect(result.current.state).toBe('expanded');
  });

  it('hides and restores the last visible state', () => {
    const { result } = renderHook(() => useSidebarState());
    act(() => result.current.toggleCollapsed());
    act(() => result.current.toggleHidden());
    expect(result.current.state).toBe('hidden');

    act(() => result.current.toggleHidden());
    expect(result.current.state).toBe('collapsed');
  });

  it('remembers the choice across mounts', () => {
    const first = renderHook(() => useSidebarState());
    act(() => first.result.current.toggleHidden());
    first.unmount();

    const { result } = renderHook(() => useSidebarState());
    expect(result.current.state).toBe('hidden');
  });
});
