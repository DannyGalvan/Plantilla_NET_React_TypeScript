import { act, render, screen } from '@testing-library/react';
import { userEvent } from '@testing-library/user-event';
import { beforeEach, describe, expect, it } from 'vitest';

import { defaultPlatformConfig, usePlatformStore } from '@/shared/platform/platform-config';

import { useTheme } from './theme-context';
import { ThemeProvider } from './theme-provider';

function Controls() {
  const { update, effectivePreset } = useTheme();
  return (
    <>
      <span>preset:{effectivePreset}</span>
      <button type="button" onClick={() => update({ mode: 'dark' })}>
        dark
      </button>
      <button
        type="button"
        onClick={() => update({ preset: 'forest', density: 'compact', primaryColor: 'oklch(0.6 0.2 30)' })}
      >
        custom
      </button>
    </>
  );
}

describe('ThemeProvider', () => {
  beforeEach(() => {
    localStorage.clear();
    document.documentElement.className = '';
    usePlatformStore.getState().setConfig({
      ...defaultPlatformConfig,
      branding: { ...defaultPlatformConfig.branding, themePreset: 'ocean', radius: '0.5rem' },
    });
  });

  it('applies the brand of the tenant by default', () => {
    render(
      <ThemeProvider>
        <Controls />
      </ThemeProvider>,
    );
    expect(document.documentElement.dataset.theme).toBe('ocean');
    expect(document.documentElement.style.getPropertyValue('--radius')).toBe('0.5rem');
  });

  it('applies and persists user preferences on top of the brand', async () => {
    render(
      <ThemeProvider>
        <Controls />
      </ThemeProvider>,
    );

    await userEvent.click(screen.getByRole('button', { name: 'dark' }));
    await userEvent.click(screen.getByRole('button', { name: 'custom' }));

    const root = document.documentElement;
    expect(root.classList.contains('dark')).toBe(true);
    expect(root.dataset.theme).toBe('forest');
    expect(root.dataset.density).toBe('compact');
    expect(root.style.getPropertyValue('--primary-override')).toBe('oklch(0.6 0.2 30)');
    expect(JSON.parse(localStorage.getItem('theme-preferences') ?? '{}')).toMatchObject({
      mode: 'dark',
      preset: 'forest',
    });
  });

  it('ignores the user primary color when the brand forbids it', () => {
    localStorage.setItem(
      'theme-preferences',
      JSON.stringify({ mode: 'light', density: 'comfortable', primaryColor: 'oklch(0.5 0.1 10)' }),
    );
    act(() =>
      usePlatformStore.getState().setConfig({
        ...defaultPlatformConfig,
        branding: {
          ...defaultPlatformConfig.branding,
          primaryColor: 'oklch(0.4 0.2 260)',
          allowUserPrimaryOverride: false,
        },
      }),
    );

    render(
      <ThemeProvider>
        <Controls />
      </ThemeProvider>,
    );
    expect(document.documentElement.style.getPropertyValue('--primary-override')).toBe('oklch(0.4 0.2 260)');
  });
});
