import { useEffect, useMemo, useState, type ReactNode } from 'react';

import type { PlatformConfig } from '../platform/platform-config';

import { ThemeContext } from './theme-context';

type BrandingSettings = PlatformConfig['branding'];

export type ThemeMode = 'light' | 'dark' | 'system';
export type ThemePreset =
  | 'default'
  | 'ocean'
  | 'forest'
  | 'sunset'
  | 'corporate';
export type ThemeDensity = 'compact' | 'comfortable';
export type ThemeFontScale = 'sm' | 'md' | 'lg';

export interface ThemeState {
  mode: ThemeMode;
  preset: ThemePreset;
  density: ThemeDensity;
  fontScale: ThemeFontScale;
  primaryOverride: string | null;
  radius: string | null;
  setMode: (mode: ThemeMode) => void;
  setPreset: (preset: ThemePreset) => void;
  setDensity: (density: ThemeDensity) => void;
  setFontScale: (scale: ThemeFontScale) => void;
  setPrimaryOverride: (oklch: string | null) => void;
  setRadius: (radius: string | null) => void;
  applyBranding: (branding: BrandingSettings) => void;
}

const STORAGE_KEY = 'brand:theme';

interface PersistedTheme {
  mode: ThemeMode;
  preset: ThemePreset;
  density: ThemeDensity;
  fontScale: ThemeFontScale;
  primaryOverride: string | null;
  radius: string | null;
}

function readPersisted(): PersistedTheme | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as PersistedTheme) : null;
  } catch {
    return null;
  }
}

function writePersisted(state: PersistedTheme): void {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
  } catch {
    // localStorage unavailable (private mode). Theme stays in-memory.
  }
}

/**
 * Resolves the brand from BrandingSettings + the persisted user overrides.
 * Applies the theme via \`data-theme\`, \`data-density\` and \`.dark\` on
 * \`<html>\` so CSS variables in tokens.css drive the rest of the app.
 */
export function ThemeProvider({ children }: { children: ReactNode }) {
  const persisted = useMemo(() => readPersisted(), []);
  const [mode, setModeState] = useState<ThemeMode>(persisted?.mode ?? 'system');
  const [preset, setPresetState] = useState<ThemePreset>(
    persisted?.preset ?? 'default',
  );
  const [density, setDensityState] = useState<ThemeDensity>(
    persisted?.density ?? 'comfortable',
  );
  const [fontScale, setFontScaleState] = useState<ThemeFontScale>(
    persisted?.fontScale ?? 'md',
  );
  const [primaryOverride, setPrimaryOverrideState] = useState<string | null>(
    persisted?.primaryOverride ?? null,
  );
  const [radius, setRadiusState] = useState<string | null>(
    persisted?.radius ?? null,
  );

  useEffect(() => {
    const html = document.documentElement;
    html.setAttribute('data-theme', preset);
    html.setAttribute('data-density', density);

    const prefersDark = window.matchMedia(
      '(prefers-color-scheme: dark)',
    ).matches;
    const effective =
      mode === 'system' ? (prefersDark ? 'dark' : 'light') : mode;
    html.classList.toggle('dark', effective === 'dark');

    if (primaryOverride) {
      html.style.setProperty('--primary', primaryOverride);
      html.style.setProperty('--sidebar-primary', primaryOverride);
    } else {
      html.style.removeProperty('--primary');
      html.style.removeProperty('--sidebar-primary');
    }

    if (radius) {
      html.style.setProperty('--radius', radius);
    } else {
      html.style.removeProperty('--radius');
    }

    html.style.fontSize =
      fontScale === 'sm' ? '14px' : fontScale === 'lg' ? '18px' : '16px';

    writePersisted({
      mode,
      preset,
      density,
      fontScale,
      primaryOverride,
      radius,
    });
  }, [mode, preset, density, fontScale, primaryOverride, radius]);

  const value = useMemo<ThemeState>(
    () => ({
      mode,
      preset,
      density,
      fontScale,
      primaryOverride,
      radius,
      setMode: setModeState,
      setPreset: setPresetState,
      setDensity: setDensityState,
      setFontScale: setFontScaleState,
      setPrimaryOverride: setPrimaryOverrideState,
      setRadius: setRadiusState,
      applyBranding(b) {
        setPresetState((b.themePreset as ThemePreset) ?? 'default');
        setPrimaryOverrideState(b.primaryColor ?? null);
        setRadiusState(b.radius ?? null);
      },
    }),
    [mode, preset, density, fontScale, primaryOverride, radius],
  );

  return (
    <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>
  );
}
