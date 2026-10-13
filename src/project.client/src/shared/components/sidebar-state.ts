import { useCallback, useEffect, useState } from 'react';

/** Desktop sidebar: full width with labels, an icon rail, or hidden. Persisted per browser. */
export type SidebarState = 'expanded' | 'collapsed' | 'hidden';

const SIDEBAR_KEY = 'app-shell:sidebar';

function readSidebar(): { state: SidebarState; lastVisible: Exclude<SidebarState, 'hidden'> } {
  try {
    const stored = JSON.parse(localStorage.getItem(SIDEBAR_KEY) ?? '{}') as Partial<ReturnType<typeof readSidebar>>;
    return {
      state: stored.state === 'collapsed' || stored.state === 'hidden' ? stored.state : 'expanded',
      lastVisible: stored.lastVisible === 'collapsed' ? 'collapsed' : 'expanded',
    };
  } catch {
    return { state: 'expanded', lastVisible: 'expanded' };
  }
}

export function useSidebarState() {
  const [value, setValue] = useState(readSidebar);

  useEffect(() => {
    try {
      localStorage.setItem(SIDEBAR_KEY, JSON.stringify(value));
    } catch {
      // Storage blocked (private mode): the choice lasts for the session only.
    }
  }, [value]);

  const toggleCollapsed = useCallback(
    () =>
      setValue((current) => {
        const next = current.state === 'expanded' ? 'collapsed' : 'expanded';
        return { state: next, lastVisible: next };
      }),
    [],
  );
  const toggleHidden = useCallback(
    () =>
      setValue((current) =>
        current.state === 'hidden' ? { ...current, state: current.lastVisible } : { ...current, state: 'hidden' },
      ),
    [],
  );

  return { state: value.state, toggleCollapsed, toggleHidden };
}
