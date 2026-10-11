import { createContext } from 'react';

import type { ThemeState } from './theme-provider';

export const ThemeContext = createContext<ThemeState | null>(null);
