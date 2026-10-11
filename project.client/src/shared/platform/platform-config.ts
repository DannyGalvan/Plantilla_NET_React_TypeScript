import axios from 'axios';

export interface TenantInfoLite {
  code: string;
  name: string;
}

export interface PlatformConfig {
  appName: string;
  tenancyMode: 'None' | 'SharedDatabase' | 'DatabasePerTenant' | 'Hybrid';
  tenant: TenantInfoLite | null;
  branding: {
    displayName: string;
    logoUrl: string | null;
    faviconUrl: string | null;
    themePreset: string;
    primaryColor: string | null;
    radius: string | null;
  };
  localization: {
    locale: string;
    timeZone: string;
    currency: string;
    dateFormat: string;
  };
  tax: {
    country: string;
    taxIdLabel: string;
    defaultRate: number;
  };
  security: {
    passwordMinLength: number;
    mfaRequired: boolean;
    sessionTimeoutMinutes: number;
  };
  features: string[];
  organizationEnabled: boolean;
}

let cached: PlatformConfig | null = null;
let inflight: Promise<PlatformConfig> | null = null;

/**
 * Loads \`/platform/config\` once per session. Called by the bootstrap routine
 * before the router mounts. The result seeds the auth store's brand and locale.
 */
export async function loadPlatformConfig(): Promise<PlatformConfig> {
  if (cached) return cached;
  if (inflight) return inflight;

  inflight = axios
    .get<PlatformConfig>('/platform/config')
    .then((res) => {
      cached = res.data;
      return cached;
    })
    .finally(() => {
      inflight = null;
    });

  return inflight;
}

export function getCachedPlatformConfig(): PlatformConfig | null {
  return cached;
}
