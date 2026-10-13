import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';

import { usePlatformStore } from '@/shared/platform/platform-config';

import { ThemeContext, type ThemeContextValue, type ThemePreferences } from './theme-context';

const STORAGE_KEY = 'theme-preferences';
const defaults: ThemePreferences = { mode: 'system', density: 'comfortable' };

function readPreferences(): ThemePreferences {
  try {
    return { ...defaults, ...(JSON.parse(localStorage.getItem(STORAGE_KEY) ?? '{}') as Partial<ThemePreferences>) };
  } catch {
    return defaults;
  }
}

function writePreferences(preferences: ThemePreferences) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(preferences));
  } catch {
    // Storage blocked (private mode): preferences last for the session only.
  }
}

function useSystemDark(): boolean {
  const query = typeof window !== 'undefined' ? window.matchMedia?.('(prefers-color-scheme: dark)') : undefined;
  const [dark, setDark] = useState(() => query?.matches ?? false);
  useEffect(() => {
    if (!query) {
      return;
    }

    const listener = (event: MediaQueryListEvent) => setDark(event.matches);
    query.addEventListener('change', listener);
    return () => query.removeEventListener('change', listener);
  }, [query]);
  return dark;
}

/**
 * Applies the theme to <html>: mode (.dark), brand preset (data-theme), radius, density and an optional primary
 * color. Precedence: user preferences, then the brand of the tenant (BrandingSettings), then the defaults.
 */
export function ThemeProvider({ children }: { children: ReactNode }) {
  const branding = usePlatformStore((state) => state.config.branding);
  const [preferences, setPreferences] = useState<ThemePreferences>(readPreferences);
  const systemDark = useSystemDark();

  const resolvedMode = preferences.mode === 'system' ? (systemDark ? 'dark' : 'light') : preferences.mode;
  const effectivePreset = preferences.preset ?? branding.themePreset;
  const canOverridePrimary = branding.allowUserPrimaryOverride;
  const primary = (canOverridePrimary ? preferences.primaryColor : undefined) ?? branding.primaryColor ?? undefined;
  const radius = preferences.radius ?? branding.radius;

  useEffect(() => {
    const root = document.documentElement;
    root.classList.toggle('dark', resolvedMode === 'dark');
    root.dataset.theme = effectivePreset;
    root.dataset.density = preferences.density;
    root.style.setProperty('--radius', radius);

    if (primary) {
      root.dataset.primaryOverride = 'true';
      root.style.setProperty('--primary-override', primary);
    } else {
      delete root.dataset.primaryOverride;
      root.style.removeProperty('--primary-override');
    }
  }, [resolvedMode, effectivePreset, preferences.density, radius, primary]);

  const update = useCallback((changes: Partial<ThemePreferences>) => {
    setPreferences((current) => {
      const next = { ...current, ...changes };
      writePreferences(next);
      return next;
    });
  }, []);

  const reset = useCallback(() => {
    writePreferences(defaults);
    setPreferences(defaults);
  }, []);

  const value = useMemo<ThemeContextValue>(
    () => ({ preferences, resolvedMode, effectivePreset, canOverridePrimary, update, reset }),
    [preferences, resolvedMode, effectivePreset, canOverridePrimary, update, reset],
  );

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}
