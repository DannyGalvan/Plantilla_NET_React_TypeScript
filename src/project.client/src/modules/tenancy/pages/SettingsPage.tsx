import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';

import { ApiError } from '@/shared/api/problem-details';
import { useQueryScope } from '@/shared/api/query-keys';
import { useAuthStore } from '@/shared/auth/auth-store';
import { usePermission } from '@/shared/auth/permissions';
import { PageHeader, QueryError, Spinner } from '@/shared/components/page';
import { loadPlatformConfig, usePlatformStore } from '@/shared/platform/platform-config';
import { Button, Card, CardContent, Input, Label, NativeSelect, Switch } from '@/shared/ui';

import { tenancyApi, type SettingScope, type SettingsSection } from '../api';

type Values = Record<string, unknown>;

function display(value: unknown): string {
  if (value === null || value === undefined) {
    return '';
  }

  if (typeof value === 'string') {
    return value;
  }

  return typeof value === 'number' || typeof value === 'boolean' ? value.toString() : JSON.stringify(value);
}

function humanize(key: string) {
  return key.replace(/([A-Z])/g, ' $1').replace(/^./, (first) => first.toUpperCase());
}

/**
 * Editor generated from the settings schema exposed by the backend: a new settings section declared by any module
 * appears here without new UI code. Empty fields inherit the value of the parent level.
 */
function SectionEditor({
  section,
  scope,
  scopeId,
}: {
  section: SettingsSection;
  scope: SettingScope;
  scopeId?: string;
}) {
  const { t } = useTranslation('tenancy');
  const queryScope = useQueryScope('tenancy', 'settings');
  const client = useQueryClient();
  const canManage = usePermission('tenancy.settings.manage');
  const value = useQuery({
    queryKey: [...queryScope, section.key, scope, scopeId],
    queryFn: () => tenancyApi.getSettings(section.key, scope, scopeId),
    enabled: scope !== 'OrganizationUnit' || Boolean(scopeId),
  });
  const [draft, setDraft] = useState<Values>({});

  useEffect(() => setDraft(value.data?.overrides ?? {}), [value.data]);

  const save = useMutation({
    mutationFn: () =>
      tenancyApi.updateSettings(
        section.key,
        scope,
        Object.fromEntries(Object.entries(draft).filter(([, v]) => v !== '' && v !== undefined)),
        scopeId,
      ),
    onSuccess: async () => {
      await client.invalidateQueries({ queryKey: queryScope });
      await loadPlatformConfig();
      toast.success(t('settings.saved'));
    },
    onError: (error) => toast.error(ApiError.from(error).message),
  });

  if (value.isLoading) {
    return <Spinner />;
  }

  if (value.error) {
    return <QueryError error={value.error} onRetry={() => void value.refetch()} />;
  }

  const effective = value.data?.effective ?? {};

  return (
    <form
      className="grid gap-4"
      onSubmit={(event) => {
        event.preventDefault();
        save.mutate();
      }}
    >
      {Object.entries(section.defaults).map(([key, defaultValue]) => {
        const id = `setting-${key}`;
        const current = draft[key];
        const inherited = t('settings.inherited', { value: display(effective[key]) });

        if (typeof defaultValue === 'boolean') {
          return (
            <Label key={key} className="justify-between font-normal">
              <span>{humanize(key)}</span>
              <Switch
                disabled={!canManage}
                checked={Boolean(current ?? effective[key])}
                onCheckedChange={(checked) => setDraft({ ...draft, [key]: checked })}
              />
            </Label>
          );
        }

        const numeric = typeof defaultValue === 'number';
        return (
          <div key={key} className="grid gap-1.5">
            <Label htmlFor={id}>{humanize(key)}</Label>
            <Input
              id={id}
              type={numeric ? 'number' : 'text'}
              disabled={!canManage && scope !== 'User'}
              placeholder={inherited}
              value={display(current)}
              onChange={(event) =>
                setDraft({
                  ...draft,
                  [key]: numeric
                    ? event.target.value === ''
                      ? undefined
                      : Number(event.target.value)
                    : event.target.value,
                })
              }
            />
          </div>
        );
      })}
      <div>
        <Button type="submit" disabled={save.isPending}>
          {t('common:actions.save')}
        </Button>
      </div>
    </form>
  );
}

export function SettingsPage() {
  const { t } = useTranslation('tenancy');
  const queryScope = useQueryScope('tenancy', 'settings');
  const schema = useQuery({ queryKey: [...queryScope, 'schema'], queryFn: tenancyApi.settingsSchema });
  const isPlatformAdmin = useAuthStore((state) => state.profile?.isPlatformAdmin ?? false);
  const organizationEnabled = usePlatformStore((state) => state.config.organizationEnabled);
  const [sectionKey, setSectionKey] = useState('branding');
  const [scope, setScope] = useState<SettingScope>('Tenant');
  const [unitId, setUnitId] = useState('');

  const section = schema.data?.find((item) => item.key === sectionKey) ?? schema.data?.[0];
  const scopes: SettingScope[] = [
    ...(isPlatformAdmin ? (['System'] as const) : []),
    'Tenant',
    ...(organizationEnabled ? (['OrganizationUnit'] as const) : []),
    ...(section?.allowUserOverride ? (['User'] as const) : []),
  ];

  return (
    <div className="space-y-6">
      <PageHeader title={t('settings.title')} description={t('settings.description')} />
      {schema.isLoading ? (
        <Spinner />
      ) : schema.error ? (
        <QueryError error={schema.error} />
      ) : (
        <Card>
          <CardContent className="grid gap-6">
            <div className="grid gap-4 sm:grid-cols-3">
              <div className="grid gap-1.5">
                <Label htmlFor="settings-section">{t('settings.section')}</Label>
                <NativeSelect
                  id="settings-section"
                  value={section?.key}
                  onChange={(event) => setSectionKey(event.target.value)}
                >
                  {schema.data?.map((item) => (
                    <option key={item.key} value={item.key}>
                      {t(`settings.sections.${item.key}`, { defaultValue: humanize(item.key) })}
                    </option>
                  ))}
                </NativeSelect>
              </div>
              <div className="grid gap-1.5">
                <Label htmlFor="settings-scope">{t('settings.scope')}</Label>
                <NativeSelect
                  id="settings-scope"
                  value={scopes.includes(scope) ? scope : 'Tenant'}
                  onChange={(event) => setScope(event.target.value as SettingScope)}
                >
                  {scopes.map((item) => (
                    <option key={item} value={item}>
                      {t(`settings.scopes.${item}`)}
                    </option>
                  ))}
                </NativeSelect>
              </div>
              {scope === 'OrganizationUnit' && (
                <div className="grid gap-1.5">
                  <Label htmlFor="settings-unit">{t('settings.unitId')}</Label>
                  <Input id="settings-unit" value={unitId} onChange={(event) => setUnitId(event.target.value)} />
                </div>
              )}
            </div>
            {section && (
              <SectionEditor
                key={`${section.key}-${scope}-${unitId}`}
                section={section}
                scope={scopes.includes(scope) ? scope : 'Tenant'}
                scopeId={unitId || undefined}
              />
            )}
          </CardContent>
        </Card>
      )}
    </div>
  );
}
