import i18n from 'i18next';
import LanguageDetector from 'i18next-browser-languagedetector';
import { initReactI18next } from 'react-i18next';

import en from './en.json';
import es from './es.json';

export const supportedLanguages = ['es', 'en'] as const;
export type Language = (typeof supportedLanguages)[number];
export type ModuleResources = Record<Language, Record<string, unknown>>;

/**
 * i18next with the shared `common` namespace (default) plus one namespace per module, registered by the module
 * manifests. Spanish is the default; the choice is remembered in localStorage.
 */
export function initI18n(moduleResources: Record<string, ModuleResources> = {}) {
  if (!i18n.isInitialized) {
    void i18n
      .use(LanguageDetector)
      .use(initReactI18next)
      .init({
        resources: { es: { common: es }, en: { common: en } },
        defaultNS: 'common',
        fallbackLng: 'es',
        supportedLngs: [...supportedLanguages],
        interpolation: { escapeValue: false },
        detection: { order: ['localStorage', 'navigator'], lookupLocalStorage: 'language', caches: ['localStorage'] },
        returnNull: false,
      });
  }

  for (const [namespace, resources] of Object.entries(moduleResources)) {
    for (const language of supportedLanguages) {
      i18n.addResourceBundle(language, namespace, resources[language], true, true);
    }
  }

  return i18n;
}

export { i18n };
