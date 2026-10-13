# Tema y marca

## Tokens

`src/project.client/src/shared/theme/tokens.css` es la única fuente de color. Define variables semánticas en OKLCH
(`--primary`, `--muted`, `--destructive`, `--success`, `--sidebar`...) en claro y oscuro, y las expone a Tailwind con
`@theme inline`. Los componentes usan solo utilidades semánticas (`bg-primary`, `text-muted-foreground`).
`pnpm tokens:check` falla si aparece una paleta cruda (`bg-blue-500`) o un hex fuera de `shared/theme`.

## Presets

`presets.css` define `ocean`, `forest`, `sunset` y `corporate` sobre el preset por defecto. Para crear uno: copia un
bloque `[data-theme='...']`, cambia sus colores y agrégalo a `presets` en `theme-context.ts`.

## Precedencia

1. Preferencias del usuario guardadas en el navegador (modo, preset, bordes, densidad y color primario si la marca lo permite).
2. Marca del tenant o unidad (`BrandingSettings` de `/platform/config`, resuelto por dominio).
3. Valores por defecto.

El color primario libre deriva el anillo y el acento con `color-mix(in oklch, ...)`. Un script estático
(`public/theme-init.js`) aplica el último tema antes de cargar React para evitar parpadeos.

## Personalizador

El botón de paleta de la barra superior abre un panel con modo (claro, oscuro, sistema), preset, color primario,
bordes, densidad (cómoda o compacta) e idioma. Los cambios se ven al instante.

## Marca por dominio

Nombre, logo, favicon, preset, color y radio se configuran en *Configuración > Marca* a nivel de tenant o unidad. El mismo
build sirve marcas distintas en `acme.miapp.com` y `globex.miapp.com`.
