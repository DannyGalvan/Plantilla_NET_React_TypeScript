import '@/shared/theme/tokens.css';

import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';

import { App } from '@/app/App';
import { activeModules, moduleResources } from '@/app/module-registry';
import { createQueryClient } from '@/app/query-client';
import { createAppRouter } from '@/app/router';
import { httpHooks } from '@/shared/api/http-client';
import { restoreSession } from '@/shared/auth/session';
import { initI18n } from '@/shared/i18n';
import { setFormatSettings } from '@/shared/lib/format';
import { setActiveModules } from '@/shared/modules/active-modules';
import { loadPlatformConfig, type PlatformConfig } from '@/shared/platform/platform-config';

function applyBranding(config: PlatformConfig) {
  document.title = config.branding.displayName;
  if (config.branding.faviconUrl) {
    document.querySelector<HTMLLinkElement>('link[rel="icon"]')?.setAttribute('href', config.branding.faviconUrl);
  }

  setFormatSettings({
    locale: config.localization.locale,
    currency: config.localization.currency,
    timeZone: config.localization.timeZone,
  });
}

async function bootstrap() {
  const i18n = initI18n(moduleResources());
  httpHooks.language = () => i18n.resolvedLanguage ?? 'es';

  // Brand, locale and features are resolved by host: one build serves every tenant and brand.
  const config = await loadPlatformConfig();
  applyBranding(config);
  if (!localStorage.getItem('language') && config.languages.includes(config.localization.locale)) {
    await i18n.changeLanguage(config.localization.locale);
  }

  const queryClient = createQueryClient();
  const modules = activeModules(config);
  setActiveModules(modules);
  const router = createAppRouter(modules);
  httpHooks.onSessionExpired = () => {
    queryClient.clear();
    void router.navigate('/login');
  };

  void restoreSession();

  createRoot(document.getElementById('root')!).render(
    <StrictMode>
      <App router={router} queryClient={queryClient} />
    </StrictMode>,
  );
}

void bootstrap();
