import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { CheckIcon, HardDriveIcon, Trash2Icon } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router';
import { toast } from 'sonner';

import { ApiError } from '@/shared/api/problem-details';
import { useQueryScope } from '@/shared/api/query-keys';
import { PageHeader } from '@/shared/components/page';
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Checkbox,
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Input,
  Label,
} from '@/shared/ui';

import { keyProviders, storageApi, type StorageProvider } from '../api';

/** Connects the tenant's own Google Drive, OneDrive, S3 bucket or Azure container and picks the default storage. */
export function StoragePage() {
  const { t } = useTranslation('files');
  const queryClient = useQueryClient();
  const scope = useQueryScope('files', 'storage');
  const [params, setParams] = useSearchParams();
  const [connecting, setConnecting] = useState<StorageProvider | null>(null);
  const providers = useQuery({ queryKey: [...scope, 'providers'], queryFn: storageApi.providers });
  const connections = useQuery({ queryKey: [...scope, 'connections'], queryFn: storageApi.connections });
  const onError = (error: unknown) => toast.error(ApiError.from(error).message);
  const refresh = () => queryClient.invalidateQueries({ queryKey: scope });
  const setDefault = useMutation({ mutationFn: storageApi.setDefault, onSuccess: refresh, onError });
  const remove = useMutation({ mutationFn: storageApi.remove, onSuccess: refresh, onError });

  // Result of the OAuth round trip (?storage=connected|failed).
  useEffect(() => {
    const result = params.get('storage');
    if (result) {
      if (result === 'connected') {
        toast.success(t('storage.connected'));
      } else {
        toast.error(t('storage.failed'));
      }

      params.delete('storage');
      setParams(params, { replace: true });
    }
  }, [params, setParams, t]);

  const usesPlatform = !connections.data?.some((connection) => connection.isDefault);

  return (
    <div className="space-y-6">
      <PageHeader title={t('storage.title')} description={t('storage.description')} />

      <div className="grid gap-4 md:grid-cols-2">
        <Card>
          <CardHeader className="flex flex-row items-start justify-between gap-2">
            <div className="space-y-1">
              <CardTitle className="flex items-center gap-2">
                <HardDriveIcon className="size-4" aria-hidden />
                {t('storage.platform')}
              </CardTitle>
              <CardDescription>{t('storage.platformHint')}</CardDescription>
            </div>
            {usesPlatform ? (
              <Badge>{t('storage.default')}</Badge>
            ) : (
              <Button variant="outline" size="sm" onClick={() => setDefault.mutate(null)}>
                {t('storage.makeDefault')}
              </Button>
            )}
          </CardHeader>
        </Card>

        {connections.data?.map((connection) => (
          <Card key={connection.id}>
            <CardHeader className="flex flex-row items-start justify-between gap-2">
              <div className="min-w-0 space-y-1">
                <CardTitle className="truncate">{connection.name}</CardTitle>
                <CardDescription className="truncate">
                  {t(`storage.providers.${connection.provider}`)}
                  {connection.account ? ` · ${connection.account}` : ''}
                </CardDescription>
              </div>
              {connection.isDefault && <Badge>{t('storage.default')}</Badge>}
            </CardHeader>
            <CardContent className="flex justify-end gap-2">
              {!connection.isDefault && (
                <Button variant="outline" size="sm" onClick={() => setDefault.mutate(connection.id)}>
                  <CheckIcon aria-hidden />
                  {t('storage.makeDefault')}
                </Button>
              )}
              <Button
                variant="ghost"
                size="icon"
                aria-label={t('common:actions.delete')}
                onClick={() => remove.mutate(connection.id)}
              >
                <Trash2Icon aria-hidden />
              </Button>
            </CardContent>
          </Card>
        ))}
      </div>

      <div className="space-y-3">
        <h2 className="text-lg font-semibold">{t('storage.connect')}</h2>
        <div className="flex flex-wrap gap-2">
          {providers.data?.map((provider) => (
            <Button
              key={provider.provider}
              variant="outline"
              disabled={!provider.available}
              title={provider.available ? undefined : t('storage.notConfigured')}
              onClick={() => setConnecting(provider)}
            >
              {t('storage.connectWith', { provider: t(`storage.providers.${provider.provider}`) })}
            </Button>
          ))}
        </div>
      </div>

      <ConnectDialog provider={connecting} onClose={() => setConnecting(null)} onConnected={refresh} />
    </div>
  );
}

function ConnectDialog({
  provider,
  onClose,
  onConnected,
}: {
  provider: StorageProvider | null;
  onClose: () => void;
  onConnected: () => Promise<void>;
}) {
  const { t } = useTranslation('files');
  const [name, setName] = useState('');
  const [isDefault, setIsDefault] = useState(true);
  const [values, setValues] = useState<Record<string, string>>({});
  const fields = provider ? keyProviders[provider.provider] : undefined;
  const label = provider ? t(`storage.providers.${provider.provider}`) : '';

  useEffect(() => {
    setName(label);
    setValues({});
    setIsDefault(true);
  }, [label]);

  const submit = useMutation({
    mutationFn: async () => {
      if (!provider) {
        return;
      }

      if (provider.usesOAuth) {
        window.location.assign(await storageApi.startOAuth(provider.provider, name, isDefault));
        return;
      }

      const pick = (keys: string[]) =>
        Object.fromEntries(keys.filter((key) => values[key]).map((key) => [key, values[key]!]));
      await storageApi.create({
        name,
        provider: provider.provider,
        settings: pick(fields?.settings ?? []),
        credentials: pick(fields?.credentials ?? []),
        isDefault,
      });
      toast.success(t('storage.connected'));
      await onConnected();
      onClose();
    },
    onError: (error) => toast.error(ApiError.from(error).message),
  });

  return (
    <Dialog open={provider !== null} onOpenChange={(open) => !open && onClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{t('storage.connectWith', { provider: label })}</DialogTitle>
        </DialogHeader>
        <form
          className="space-y-4"
          onSubmit={(event) => {
            event.preventDefault();
            submit.mutate();
          }}
        >
          <div className="space-y-2">
            <Label htmlFor="connection-name">{t('storage.name')}</Label>
            <Input
              id="connection-name"
              value={name}
              required
              maxLength={128}
              onChange={(event) => setName(event.target.value)}
            />
          </div>
          {[...(fields?.settings ?? []), ...(fields?.credentials ?? [])].map((key) => (
            <div key={key} className="space-y-2">
              <Label htmlFor={`connection-${key}`}>{key}</Label>
              <Input
                id={`connection-${key}`}
                type={fields?.credentials.includes(key) ? 'password' : 'text'}
                autoComplete="off"
                value={values[key] ?? ''}
                onChange={(event) => setValues((current) => ({ ...current, [key]: event.target.value }))}
              />
            </div>
          ))}
          {fields && <p className="text-xs text-muted-foreground">{t('storage.secretHint')}</p>}
          <Label className="flex items-center gap-2 font-normal">
            <Checkbox checked={isDefault} onCheckedChange={(checked) => setIsDefault(Boolean(checked))} />
            {t('storage.makeDefault')}
          </Label>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose}>
              {t('common:actions.cancel')}
            </Button>
            <Button type="submit" disabled={submit.isPending}>
              {t('storage.connect')}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
