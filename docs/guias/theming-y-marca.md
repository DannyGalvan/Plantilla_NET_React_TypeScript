# Theming y marca

> Cómo personalizar marca por tenant sin tocar el código central.

## Anatomía de los tokens

`src/shared/theme/tokens.css` define:

- **Variables semánticas**: `--background`, `--foreground`, `--card`,
  `--primary`, `--destructive`, etc.
- **Light + dark**: `.dark` redefine todas las variables.
- **Brand presets**: `[data-theme="default"]`, `[data-theme="ocean"]`,
  `[data-theme="forest"]`, `[data-theme="sunset"]`, `[data-theme="corporate"]`.
  Cada preset redefine `--primary` (y derivados como `--sidebar-primary`).
- **Density presets**: `[data-density="compact"]`, `[data-density="comfortable"]`
  cambian `--spacing`.
- **Shape**: `--radius` controla el redondeo de botones, cards, inputs.
- **Chart palette**: `--chart-1` ... `--chart-5` para gráficos categóricos.

## Cómo agrega un nuevo preset de marca

1. Edita `src/shared/theme/tokens.css`:

   ```css
   [data-theme="mi-marca"] {
     --primary: oklch(0.55 0.22 264);   /* tu color */
     --primary-foreground: oklch(0.98 0 0);
     --sidebar-primary: var(--primary);
     /* opcional: --ring derivado */
     --ring: var(--primary);
   }
   ```

2. Agrega `'mi-marca'` al array `presets` en `src/shared/theme/theme-customizer.tsx`:

   ```ts
   const presets = ['default', 'ocean', 'forest', 'sunset', 'corporate', 'mi-marca'] as const;
   ```

3. Expón el preset en la configuración del tenant vía `BrandingSettings`:

   ```jsonc
   { "branding": { "themePreset": "mi-marca" } }
   ```

4. El `bootstrap` (en `app/main.tsx`, cuando llegue la Fase 8 slice 2) lee
   `/platform/config` y llama a `theme.applyBranding(...)`, que setea
   `setPreset`, `setPrimaryOverride`, `setRadius`.

## Override de color primario en runtime

`ThemeProvider.applyBranding(branding)` aplica los overrides del tenant
sobre el preset:

```ts
theme.applyBranding({
  themePreset: 'ocean',
  primaryColor: 'oklch(0.62 0.18 220)',
  radius: '0.75rem',
});
```

El backend valida el color: formato OKLCH o hex, y contraste AA contra
`--primary-foreground`. Si no pasa, devuelve 400 `branding.invalid_contrast`.

## Dark mode

`ThemeProvider.mode` admite `light`, `dark` o `system`. En `system`, sigue
`prefers-color-scheme` y actualiza en tiempo real con un listener
`matchMedia`. El usuario puede fijar el modo desde `ThemeCustomizer`.

## Density

`data-density="compact"` reduce `--spacing` y achica paddings. Útil
para pantallas densas (back-office con muchos datos) o para usuarios
con baja visión que prefieren menos espacio entre controles.

## Cómo agrega un chart color

```css
:root {
  --chart-6: oklch(0.65 0.22 280);
  --chart-7: oklch(0.7 0.18 150);
}
.dark {
  --chart-6: oklch(0.75 0.22 280);
  --chart-7: oklch(0.78 0.18 150);
}
```

Las clases `bg-chart-6`, `text-chart-6` se generan automáticamente via el
mapping en `tailwind.config.js` (cuando llegue Fase 7 slice 2).

## Cómo verifica que funciona

- Cambia el preset en `ThemeCustomizer` y observa cómo `--primary` cambia
  en `<html>`.
- Cambia el primaryOverride y comprueba que el `color-mix(in oklch, ...)`
  genera el `--ring` derivado.
- Cambia el radio y verifica que `border-radius` en `.btn` se actualiza.
- El test de contraste AA en CI ejecuta `scripts/check-contrast.mjs` (Fase 7
  slice 2) contra cada preset.