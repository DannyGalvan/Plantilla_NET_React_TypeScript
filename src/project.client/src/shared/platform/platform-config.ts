import { z } from 'zod';
import { create } from 'zustand';

import { http } from '@/shared/api/http-client';

export const platformConfigSchema = z.object({
  appName: z.string(),
  tenancyMode: z.enum(['None', 'SharedDatabase', 'DatabasePerTenant', 'Hybrid']),
  tenant: z.object({ id: z.string(), code: z.string(), name: z.string() }).nullable(),
  branding: z.object({
    displayName: z.string(),
    logoUrl: z.string().nullable(),
    faviconUrl: z.string().nullable(),
    themePreset: z.string(),
    primaryColor: z.string().nullable(),
    radius: z.string(),
    allowUserPrimaryOverride: z.boolean(),
  }),
  localization: z.object({
    locale: z.string(),
    timeZone: z.string(),
    currency: z.string(),
    dateFormat: z.string(),
  }),
  features: z.array(z.string()),
  modules: z.array(z.string()),
  organizationEnabled: z.boolean(),
  languages: z.array(z.string()),
});

export type PlatformConfig = z.infer<typeof platformConfigSchema>;
export type Branding = PlatformConfig['branding'];

/** Fallback when the API is not reachable (first paint, tests). Overridden by /platform/config. */
export const defaultPlatformConfig: PlatformConfig = {
  appName: 'Plantilla',
  tenancyMode: 'None',
  tenant: null,
  branding: {
    displayName: 'Plantilla',
    logoUrl: null,
    faviconUrl: null,
    themePreset: 'default',
    primaryColor: null,
    radius: '0.625rem',
    allowUserPrimaryOverride: true,
  },
  localization: { locale: 'es', timeZone: 'UTC', currency: 'USD', dateFormat: 'dd/MM/yyyy' },
  features: [],
  modules: [],
  organizationEnabled: false,
  languages: ['es', 'en'],
};

interface PlatformState {
  config: PlatformConfig;
  setConfig: (config: PlatformConfig) => void;
}

export const usePlatformStore = create<PlatformState>((set) => ({
  config: defaultPlatformConfig,
  setConfig: (config) => set({ config }),
}));

/** Loads the configuration resolved by host (brand, locale, features). Never throws. */
export async function loadPlatformConfig(): Promise<PlatformConfig> {
  try {
    const config = platformConfigSchema.parse((await http.get('/platform/config')).data);
    usePlatformStore.getState().setConfig(config);
    return config;
  } catch {
    return usePlatformStore.getState().config;
  }
}

export function useFeature(feature: string | undefined): boolean {
  return usePlatformStore((state) => !feature || state.config.features.includes(feature));
}
