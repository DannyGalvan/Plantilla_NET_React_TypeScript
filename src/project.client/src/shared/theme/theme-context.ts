import { createContext, useContext } from 'react';

export type ThemeMode = 'light' | 'dark' | 'system';
export type Density = 'comfortable' | 'compact';

/** Preferences of the user. Unset values fall back to the brand of the tenant. */
export interface ThemePreferences {
  mode: ThemeMode;
  preset?: string;
  radius?: string;
  density: Density;
  primaryColor?: string;
}

export interface ThemeContextValue {
  preferences: ThemePreferences;
  resolvedMode: 'light' | 'dark';
  effectivePreset: string;
  canOverridePrimary: boolean;
  update: (changes: Partial<ThemePreferences>) => void;
  reset: () => void;
}

export const presets = [
  { id: 'default', labelKey: 'theme.presets.default' },
  { id: 'ocean', labelKey: 'theme.presets.ocean' },
  { id: 'forest', labelKey: 'theme.presets.forest' },
  { id: 'sunset', labelKey: 'theme.presets.sunset' },
  { id: 'corporate', labelKey: 'theme.presets.corporate' },
] as const;

export const radii = ['0rem', '0.375rem', '0.625rem', '1rem'] as const;

export const ThemeContext = createContext<ThemeContextValue | null>(null);

export function useTheme(): ThemeContextValue {
  const value = useContext(ThemeContext);
  if (!value) {
    throw new Error('useTheme must be used inside <ThemeProvider>.');
  }

  return value;
}
