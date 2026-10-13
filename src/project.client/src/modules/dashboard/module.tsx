import type { ModuleManifest } from '@/shared/modules/manifest';

import { DashboardPage } from './DashboardPage';

const manifest: ModuleManifest = {
  id: 'dashboard',
  i18n: {
    es: {
      welcome: 'Hola, {{name}}',
      gettingStarted: 'Primeros pasos',
      hint1:
        'Usa el selector de la barra superior para cambiar de organización o de unidad; los datos se filtran automáticamente.',
      hint2:
        'Personaliza colores, bordes, densidad e idioma con el botón de paleta. Cada marca puede definir su propio tema.',
    },
    en: {
      welcome: 'Hello, {{name}}',
      gettingStarted: 'Getting started',
      hint1: 'Use the selector in the top bar to switch organization or unit; data is filtered automatically.',
      hint2:
        'Customize colors, corners, density and language with the palette button. Every brand can define its own theme.',
    },
  },
  routes: [{ index: true, element: <DashboardPage />, handle: { titleKey: 'nav.dashboard' } }],
};

export default manifest;
