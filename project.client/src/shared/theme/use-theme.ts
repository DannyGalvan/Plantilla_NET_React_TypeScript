import { useContext } from 'react';

import { ThemeContext } from './theme-context';
import type { ThemeState } from './theme-provider';

export function useTheme(): ThemeState {
  const ctx = useContext(ThemeContext);
  if (!ctx) throw new Error('useTheme must be used inside ThemeProvider');
  return ctx;
}
