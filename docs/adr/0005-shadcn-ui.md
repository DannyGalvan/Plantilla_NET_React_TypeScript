# ADR 0005 — shadcn/ui + Radix + Tailwind v4 con tokens CSS en OKLCH

- **Estado:** aceptado.
- **Fecha:** 2026-10-11.
- **Contexto:** necesitamos un sistema de diseño único por build que cubra
  varios presets de marca. HeroUI se descartaba por mezclar v2 y v3 y por
  no tener una API propia de tokens.
- **Decisión:** tokens CSS en OKLCH como única fuente de color. shadcn/ui
  + Radix para accesibilidad. Tailwind v4 con `@theme inline` para que el
  `--primary` derive el resto de la escala.
- **Consecuencias:**
  - Cambio de marca sin rebuild (BrandingSettings por tenant).
  - Tema claro/oscuro/auto, preset (default/ocean/forest/sunset/corporate),
    density (compact/comfortable), radius y color primario overridable
    desde `ThemeCustomizer`.
  - `primaryOverride` se valida en el servidor con contraste AA antes de
    aceptarse.
- **Alternativas descartadas:** Material UI (overhead), Bootstrap (exceso
  de overrides), HeroUI (mixing v2/v3, demo obsoleta).