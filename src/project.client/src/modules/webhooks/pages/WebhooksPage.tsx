import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { PlusIcon, SendIcon, Trash2Icon } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';

import { ApiError } from '@/shared/api/problem-details';
import { useQueryScope } from '@/shared/api/query-keys';
import { EmptyState, PageHeader } from '@/shared/components/page';
import { formatDate } from '@/shared/lib/format';
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
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Input,
  Label,
} from '@/shared/ui';

import { webhooksApi, type Subscription } from '../api';

export function WebhooksPage() {
  const { t } = useTranslation('webhooks');
  const scope = useQueryScope('webhooks');
  const subscriptions = useQuery({ queryKey: [...scope, 'subscriptions'], queryFn: webhooksApi.subscriptions });
  const [creating, setCreating] = useState(false);
  const [secret, setSecret] = useState<string | null>(null);

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('title')}
        description={t('description')}
        actions={
          <Button onClick={() => setCreating(true)}>
            <PlusIcon aria-hidden />
            {t('new')}
          </Button>
        }
      />
      {subscriptions.data?.length ? (
        subscriptions.data.map((subscription) => (
          <SubscriptionCard key={subscription.id} subscription={subscription} scope={scope} />
        ))
      ) : (
        <EmptyState title={t('title')} description={t('description')} />
      )}
      <CreateDialog
        open={creating}
        scope={scope}
        onClose={() => setCreating(false)}
        onCreated={(value) => {
          setCreating(false);
          setSecret(value);
        }}
      />
      <Dialog open={secret !== null} onOpenChange={(open) => !open && setSecret(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('secretTitle')}</DialogTitle>
            <DialogDescription>{t('secretHint')}</DialogDescription>
          </DialogHeader>
          <Input readOnly className="font-mono" value={secret ?? ''} onFocus={(event) => event.target.select()} />
          <DialogFooter>
            <Button onClick={() => setSecret(null)}>{t('common:actions.close')}</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

function CreateDialog({
  open,
  scope,
  onClose,
  onCreated,
}: {
  open: boolean;
  scope: readonly unknown[];
  onClose: () => void;
  onCreated: (secret: string) => void;
}) {
  const { t } = useTranslation('webhooks');
  const queryClient = useQueryClient();
  const events = useQuery({ queryKey: [...scope, 'events'], queryFn: webhooksApi.events, enabled: open });
  const [name, setName] = useState('');
  const [url, setUrl] = useState('https://');
  const [selected, setSelected] = useState<string[]>(['*']);
  const create = useMutation({
    mutationFn: () => webhooksApi.create({ name, url, events: selected, isActive: true }),
    onSuccess: async (result) => {
      await queryClient.invalidateQueries({ queryKey: scope });
      setName('');
      setUrl('https://');
      setSelected(['*']);
      onCreated(result.secret);
    },
    onError: (error) => toast.error(ApiError.from(error).message),
  });
  const toggle = (event: string, checked: boolean) =>
    setSelected((current) => {
      const rest = current.filter((value) => value !== event && value !== '*');
      return checked ? [...rest, event] : rest;
    });

  return (
    <Dialog open={open} onOpenChange={(value) => !value && onClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{t('new')}</DialogTitle>
        </DialogHeader>
        <form
          className="space-y-4"
          onSubmit={(event) => {
            event.preventDefault();
            create.mutate();
          }}
        >
          <div className="space-y-2">
            <Label htmlFor="webhook-name">{t('name')}</Label>
            <Input
              id="webhook-name"
              required
              maxLength={128}
              value={name}
              onChange={(event) => setName(event.target.value)}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="webhook-url">{t('url')}</Label>
            <Input id="webhook-url" required type="url" value={url} onChange={(event) => setUrl(event.target.value)} />
          </div>
          <fieldset className="space-y-2">
            <legend className="text-sm font-medium">{t('events')}</legend>
            <Label className="flex items-center gap-2 font-normal">
              <Checkbox
                checked={selected.includes('*')}
                onCheckedChange={(checked) => setSelected(checked ? ['*'] : [])}
              />
              {t('allEvents')}
            </Label>
            {events.data?.map((event) => (
              <Label key={event.name} className="flex items-start gap-2 font-normal">
                <Checkbox
                  checked={selected.includes(event.name)}
                  disabled={selected.includes('*')}
                  onCheckedChange={(checked) => toggle(event.name, Boolean(checked))}
                />
                <span>
                  <span className="font-mono text-xs">{event.name}</span>
                  {event.description && (
                    <span className="block text-xs text-muted-foreground">{event.description}</span>
                  )}
                </span>
              </Label>
            ))}
          </fieldset>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose}>
              {t('common:actions.cancel')}
            </Button>
            <Button type="submit" disabled={create.isPending || selected.length === 0}>
              {t('common:actions.create')}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function SubscriptionCard({ subscription, scope }: { subscription: Subscription; scope: readonly unknown[] }) {
  const { t } = useTranslation('webhooks');
  const queryClient = useQueryClient();
  const [showDeliveries, setShowDeliveries] = useState(false);
  const deliveries = useQuery({
    queryKey: [...scope, subscription.id, 'deliveries'],
    queryFn: () => webhooksApi.deliveries(subscription.id),
    enabled: showDeliveries,
    refetchInterval: showDeliveries ? 5_000 : false,
  });
  const onError = (error: unknown) => toast.error(ApiError.from(error).message);
  const refresh = () => queryClient.invalidateQueries({ queryKey: scope });
  const ping = useMutation({
    mutationFn: () => webhooksApi.ping(subscription.id),
    onSuccess: () => {
      toast.success(t('pinged'));
      setShowDeliveries(true);
    },
    onError,
  });
  const toggle = useMutation({
    mutationFn: (isActive: boolean) => webhooksApi.update(subscription.id, { ...subscription, isActive }),
    onSuccess: refresh,
    onError,
  });
  const remove = useMutation({ mutationFn: () => webhooksApi.remove(subscription.id), onSuccess: refresh, onError });
  const retry = useMutation({ mutationFn: webhooksApi.retry, onSuccess: () => void deliveries.refetch(), onError });

  return (
    <Card>
      <CardHeader className="flex flex-row items-start justify-between gap-2">
        <div className="min-w-0 space-y-1">
          <CardTitle className="truncate text-base">{subscription.name}</CardTitle>
          <CardDescription className="truncate font-mono text-xs">{subscription.url}</CardDescription>
        </div>
        <Label className="flex items-center gap-2 font-normal">
          <Checkbox checked={subscription.isActive} onCheckedChange={(checked) => toggle.mutate(Boolean(checked))} />
          {t('active')}
        </Label>
      </CardHeader>
      <CardContent className="space-y-3 text-sm">
        <div className="flex flex-wrap gap-1">
          {subscription.events.map((event) => (
            <Badge key={event} variant="secondary" className="font-mono">
              {event === '*' ? t('allEvents') : event}
            </Badge>
          ))}
        </div>
        <div className="flex flex-wrap justify-end gap-2">
          <Button variant="ghost" size="sm" onClick={() => setShowDeliveries((value) => !value)}>
            {t('deliveries')}
          </Button>
          <Button variant="outline" size="sm" disabled={ping.isPending} onClick={() => ping.mutate()}>
            <SendIcon aria-hidden />
            {t('ping')}
          </Button>
          <Button variant="ghost" size="icon" aria-label={t('common:actions.delete')} onClick={() => remove.mutate()}>
            <Trash2Icon aria-hidden />
          </Button>
        </div>
        {showDeliveries && (
          <ul className="divide-y rounded-md border text-xs">
            {deliveries.data?.length ? (
              deliveries.data.map((delivery) => (
                <li key={delivery.id} className="flex flex-wrap items-center justify-between gap-2 px-3 py-2">
                  <span className="font-mono">{delivery.event}</span>
                  <span className="text-muted-foreground">{formatDate(delivery.createdAt)}</span>
                  <span className="text-muted-foreground">{t('attempts', { count: delivery.attempts })}</span>
                  <span className={delivery.status === 'Delivered' ? '' : 'text-destructive'}>
                    {t(`statuses.${delivery.status}`)}
                    {delivery.responseStatus ? ` (${delivery.responseStatus})` : ''}
                  </span>
                  {delivery.status !== 'Delivered' && (
                    <Button variant="link" size="sm" className="h-auto p-0" onClick={() => retry.mutate(delivery.id)}>
                      {t('retry')}
                    </Button>
                  )}
                </li>
              ))
            ) : (
              <li className="px-3 py-2 text-muted-foreground">{t('noDeliveries')}</li>
            )}
          </ul>
        )}
      </CardContent>
    </Card>
  );
}
