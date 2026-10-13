import { useQuery, useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';
import { Building2Icon, NetworkIcon } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';

import { http } from '@/shared/api/http-client';
import { ApiError } from '@/shared/api/problem-details';
import { useAuthStore } from '@/shared/auth/auth-store';
import { authApi } from '@/shared/auth/session';
import { NativeSelect } from '@/shared/ui';

import { usePlatformStore } from './platform-config';

const unitSchema = z.object({ id: z.string(), name: z.string(), depth: z.number(), typeCode: z.string() });

/**
 * Tenant and organization unit selector. Switching issues a new token, clears every cached query (no data of the
 * previous context can leak into the screen) and reloads the profile and permissions.
 */
export function ContextSwitcher() {
  const { t } = useTranslation();
  const client = useQueryClient();
  const profile = useAuthStore((state) => state.profile);
  const organizationEnabled = usePlatformStore((state) => state.config.organizationEnabled);

  const units = useQuery({
    queryKey: [profile?.tenant?.id, 'me', 'units'],
    queryFn: async () => z.array(unitSchema).parse((await http.get('/me/units')).data),
    enabled: organizationEnabled && Boolean(profile),
    staleTime: 60_000,
  });

  if (!profile) {
    return null;
  }

  const switchTo = async (tenantId: string, unitId: string | null) => {
    try {
      await authApi.switchContext(tenantId, unitId);
      client.clear();
      await authApi.loadProfile();
      toast.success(t('context.switched'));
    } catch (error) {
      toast.error(ApiError.from(error).message);
    }
  };

  const showTenants = profile.tenants.length > 1;
  const showUnits = organizationEnabled && (units.data?.length ?? 0) > 0;

  return (
    <div className="flex items-center gap-2">
      {showTenants && (
        <label className="flex items-center gap-1.5">
          <Building2Icon className="size-4 text-muted-foreground" aria-hidden />
          <span className="sr-only">{t('context.tenant')}</span>
          <NativeSelect
            className="h-8 max-w-44"
            value={profile.tenant?.id ?? ''}
            onChange={(event) => void switchTo(event.target.value, null)}
          >
            {profile.tenants.map((tenant) => (
              <option key={tenant.id} value={tenant.id}>
                {tenant.name}
              </option>
            ))}
          </NativeSelect>
        </label>
      )}
      {showUnits && profile.tenant && (
        <label className="flex items-center gap-1.5">
          <NetworkIcon className="size-4 text-muted-foreground" aria-hidden />
          <span className="sr-only">{t('context.unit')}</span>
          <NativeSelect
            className="h-8 max-w-52"
            value={profile.organizationUnitId ?? ''}
            onChange={(event) => void switchTo(profile.tenant!.id, event.target.value || null)}
          >
            <option value="">{t('context.allUnits')}</option>
            {units.data?.map((unit) => (
              <option key={unit.id} value={unit.id}>
                {`${'  '.repeat(unit.depth)}${unit.name}`}
              </option>
            ))}
          </NativeSelect>
        </label>
      )}
    </div>
  );
}
