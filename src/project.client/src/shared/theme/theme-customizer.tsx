import { MonitorIcon, MoonIcon, PaletteIcon, SunIcon } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { cn } from '@/shared/lib/cn';
import { Button, Input, Label, Sheet, SheetContent, SheetDescription, SheetTitle, SheetTrigger } from '@/shared/ui';

import { presets, radii, useTheme, type ThemeMode } from './theme-context';

const modes: { id: ThemeMode; icon: typeof SunIcon }[] = [
  { id: 'light', icon: SunIcon },
  { id: 'dark', icon: MoonIcon },
  { id: 'system', icon: MonitorIcon },
];

/** Side panel to customize mode, brand preset, primary color, radius and density, with live preview. */
export function ThemeCustomizer() {
  const { t, i18n } = useTranslation();
  const { preferences, effectivePreset, canOverridePrimary, update, reset } = useTheme();

  return (
    <Sheet>
      <SheetTrigger asChild>
        <Button variant="ghost" size="icon" aria-label={t('theme.customize')}>
          <PaletteIcon />
        </Button>
      </SheetTrigger>
      <SheetContent>
        <div className="space-y-1">
          <SheetTitle>{t('theme.customize')}</SheetTitle>
          <SheetDescription>{t('theme.description')}</SheetDescription>
        </div>

        <section className="space-y-2">
          <Label>{t('theme.mode')}</Label>
          <div className="grid grid-cols-3 gap-2">
            {modes.map(({ id, icon: Icon }) => (
              <Button
                key={id}
                variant={preferences.mode === id ? 'default' : 'outline'}
                size="sm"
                onClick={() => update({ mode: id })}
              >
                <Icon />
                {t(`theme.modes.${id}`)}
              </Button>
            ))}
          </div>
        </section>

        <section className="space-y-2">
          <Label>{t('theme.preset')}</Label>
          <div className="grid grid-cols-2 gap-2">
            {presets.map((preset) => (
              <button
                key={preset.id}
                type="button"
                data-theme={preset.id}
                onClick={() => update({ preset: preset.id })}
                className={cn(
                  'flex items-center gap-2 rounded-md border p-2 text-left text-sm transition-colors hover:bg-accent',
                  effectivePreset === preset.id && 'border-primary ring-2 ring-ring/40',
                )}
              >
                <span className="size-4 rounded-full bg-primary" />
                {t(preset.labelKey)}
              </button>
            ))}
          </div>
        </section>

        {canOverridePrimary && (
          <section className="space-y-2">
            <Label htmlFor="theme-primary">{t('theme.primaryColor')}</Label>
            <div className="flex gap-2">
              <Input
                id="theme-primary"
                type="color"
                className="h-control w-16 p-1"
                value={preferences.primaryColor ?? '#4f46e5'}
                onChange={(event) => update({ primaryColor: event.target.value })}
              />
              <Button
                variant="outline"
                size="sm"
                className="h-control"
                onClick={() => update({ primaryColor: undefined })}
              >
                {t('theme.useBrandColor')}
              </Button>
            </div>
          </section>
        )}

        <section className="space-y-2">
          <Label>{t('theme.radius')}</Label>
          <div className="grid grid-cols-4 gap-2">
            {radii.map((radius) => (
              <Button
                key={radius}
                variant={preferences.radius === radius ? 'default' : 'outline'}
                size="sm"
                onClick={() => update({ radius })}
              >
                {radius.replace('rem', '')}
              </Button>
            ))}
          </div>
        </section>

        <section className="space-y-2">
          <Label>{t('theme.density')}</Label>
          <div className="grid grid-cols-2 gap-2">
            {(['comfortable', 'compact'] as const).map((density) => (
              <Button
                key={density}
                variant={preferences.density === density ? 'default' : 'outline'}
                size="sm"
                onClick={() => update({ density })}
              >
                {t(`theme.densities.${density}`)}
              </Button>
            ))}
          </div>
        </section>

        <section className="space-y-2">
          <Label>{t('theme.language')}</Label>
          <div className="grid grid-cols-2 gap-2">
            {['es', 'en'].map((language) => (
              <Button
                key={language}
                variant={i18n.resolvedLanguage === language ? 'default' : 'outline'}
                size="sm"
                onClick={() => void i18n.changeLanguage(language)}
              >
                {t(`languages.${language}`)}
              </Button>
            ))}
          </div>
        </section>

        <Button variant="ghost" onClick={reset}>
          {t('theme.reset')}
        </Button>
      </SheetContent>
    </Sheet>
  );
}
