import { useId, useState, type ReactNode } from 'react';

import { Button } from '../ui/button';
import { Sheet } from '../ui/dialog';
import { Input } from '../ui/input';

import { useTheme } from './use-theme';

const presets = ['default', 'ocean', 'forest', 'sunset', 'corporate'] as const;
const modes = ['light', 'dark', 'system'] as const;
const densities = ['compact', 'comfortable'] as const;
const fontScales = ['sm', 'md', 'lg'] as const;

export function ThemeCustomizer({ trigger }: { trigger?: ReactNode }) {
  const [open, setOpen] = useState(false);
  const theme = useTheme();
  const baseId = useId();
  const id = (suffix: string) => `${baseId}-${suffix}`;

  return (
    <>
      {trigger ? (
        <button
          style={{
            background: 'transparent',
            border: 0,
            padding: 0,
            cursor: 'inherit',
            display: 'inline-flex',
          }}
          type="button"
          onClick={() => setOpen(true)}
        >
          {trigger}
        </button>
      ) : (
        <Button size="sm" variant="outline" onClick={() => setOpen(true)}>
          Tema
        </Button>
      )}
      <Sheet open={open} title="Tema y marca" onOpenChange={setOpen}>
        <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
          <label
            htmlFor={id('mode')}
            style={{ display: 'flex', flexDirection: 'column', gap: '0.25rem' }}
          >
            <span>Modo</span>
            <select
              className="widget-input"
              id={id('mode')}
              style={{
                padding: '0.5rem',
                borderRadius: 'var(--radius)',
                border: '1px solid var(--input)',
              }}
              value={theme.mode}
              onChange={(e) =>
                theme.setMode(e.target.value as (typeof modes)[number])
              }
            >
              {modes.map((m) => (
                <option key={m} value={m}>
                  {m}
                </option>
              ))}
            </select>
          </label>
          <label
            htmlFor={id('preset')}
            style={{ display: 'flex', flexDirection: 'column', gap: '0.25rem' }}
          >
            <span>Preset</span>
            <select
              id={id('preset')}
              style={{
                padding: '0.5rem',
                borderRadius: 'var(--radius)',
                border: '1px solid var(--input)',
              }}
              value={theme.preset}
              onChange={(e) =>
                theme.setPreset(e.target.value as (typeof presets)[number])
              }
            >
              {presets.map((p) => (
                <option key={p} value={p}>
                  {p}
                </option>
              ))}
            </select>
          </label>
          <label
            htmlFor={id('density')}
            style={{ display: 'flex', flexDirection: 'column', gap: '0.25rem' }}
          >
            <span>Densidad</span>
            <select
              id={id('density')}
              style={{
                padding: '0.5rem',
                borderRadius: 'var(--radius)',
                border: '1px solid var(--input)',
              }}
              value={theme.density}
              onChange={(e) =>
                theme.setDensity(e.target.value as (typeof densities)[number])
              }
            >
              {densities.map((d) => (
                <option key={d} value={d}>
                  {d}
                </option>
              ))}
            </select>
          </label>
          <label
            htmlFor={id('font')}
            style={{ display: 'flex', flexDirection: 'column', gap: '0.25rem' }}
          >
            <span>Tamaño de fuente</span>
            <select
              id={id('font')}
              style={{
                padding: '0.5rem',
                borderRadius: 'var(--radius)',
                border: '1px solid var(--input)',
              }}
              value={theme.fontScale}
              onChange={(e) =>
                theme.setFontScale(
                  e.target.value as (typeof fontScales)[number],
                )
              }
            >
              {fontScales.map((s) => (
                <option key={s} value={s}>
                  {s}
                </option>
              ))}
            </select>
          </label>
          <label
            htmlFor={id('primary')}
            style={{ display: 'flex', flexDirection: 'column', gap: '0.25rem' }}
          >
            <span>Color primario (OKLCH)</span>
            <Input
              id={id('primary')}
              placeholder="oklch(0.55 0.22 264)"
              value={theme.primaryOverride ?? ''}
              onChange={(e) => theme.setPrimaryOverride(e.target.value || null)}
            />
          </label>
          <label
            htmlFor={id('radius')}
            style={{ display: 'flex', flexDirection: 'column', gap: '0.25rem' }}
          >
            <span>Radio (--radius)</span>
            <Input
              id={id('radius')}
              placeholder="0.5rem"
              value={theme.radius ?? ''}
              onChange={(e) => theme.setRadius(e.target.value || null)}
            />
          </label>
          <div
            style={{
              display: 'flex',
              gap: '0.5rem',
              justifyContent: 'flex-end',
            }}
          >
            <Button
              variant="ghost"
              onClick={() => theme.setPrimaryOverride(null)}
            >
              Reset color
            </Button>
            <Button onClick={() => setOpen(false)}>Listo</Button>
          </div>
        </div>
      </Sheet>
    </>
  );
}
