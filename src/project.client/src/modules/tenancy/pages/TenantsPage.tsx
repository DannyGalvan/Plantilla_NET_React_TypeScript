import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import type { ColumnDef } from '@tanstack/react-table';
import { DatabaseIcon, PlusIcon, SlidersHorizontalIcon, XIcon } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { z } from 'zod';

import { useCrudList } from '@/shared/api/crud-api';
import { ApiError } from '@/shared/api/problem-details';
import { useQueryScope } from '@/shared/api/query-keys';
import { Can, usePermission } from '@/shared/auth/permissions';
import { DataTable, toListParams, useTableState } from '@/shared/components/data-table';
import { applyServerErrors, fieldProps, FormField } from '@/shared/components/form';
import { PageHeader } from '@/shared/components/page';
import { formatDate } from '@/shared/lib/format';
import {
  Badge,
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Input,
  Label,
  NativeSelect,
  Switch,
} from '@/shared/ui';

import { tenancyApi, type Tenant } from '../api';

const createSchema = z.object({
  code: z
    .string()
    .trim()
    .min(1)
    .max(64)
    .regex(/^[a-z0-9]([a-z0-9-]*[a-z0-9])?$/),
  name: z.string().trim().min(1).max(200),
  isolation: z.enum(['Shared', 'Dedicated']),
  databaseProvider: z.string().optional(),
  connectionSecretRef: z.string().optional(),
  plan: z.string().optional(),
  primaryDomain: z.string().optional(),
});
type CreateForm = z.infer<typeof createSchema>;

function CreateTenantDialog({ open, onOpenChange }: { open: boolean; onOpenChange: (open: boolean) => void }) {
  const { t } = useTranslation('tenancy');
  const scope = useQueryScope('tenancy', 'tenants');
  const client = useQueryClient();
  const form = useForm<CreateForm>({ resolver: zodResolver(createSchema), defaultValues: { isolation: 'Shared' } });
  const errors = form.formState.errors;
  const dedicated = form.watch('isolation') === 'Dedicated';

  useEffect(() => {
    if (open) {
      form.reset({
        code: '',
        name: '',
        isolation: 'Shared',
        databaseProvider: '',
        connectionSecretRef: '',
        plan: '',
        primaryDomain: '',
      });
    }
  }, [open, form]);

  const save = useMutation({
    mutationFn: (values: CreateForm) =>
      tenancyApi.create({
        ...values,
        databaseProvider: values.databaseProvider || undefined,
        connectionSecretRef: values.connectionSecretRef || undefined,
        plan: values.plan || undefined,
        primaryDomain: values.primaryDomain || undefined,
      }),
    onSuccess: async () => {
      await client.invalidateQueries({ queryKey: scope });
      toast.success(t('common:common.created'));
      onOpenChange(false);
    },
    onError: (error) => applyServerErrors(form, error),
  });

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{t('tenants.new')}</DialogTitle>
        </DialogHeader>
        <form
          className="grid gap-4"
          onSubmit={(event) => void form.handleSubmit((values) => save.mutate(values))(event)}
          noValidate
        >
          <div className="grid gap-4 sm:grid-cols-2">
            <FormField id="code" label={t('tenants.code')} error={errors.code?.message}>
              <Input {...fieldProps('code', errors.code?.message)} {...form.register('code')} />
            </FormField>
            <FormField id="name" label={t('common:common.name')} error={errors.name?.message}>
              <Input {...fieldProps('name', errors.name?.message)} {...form.register('name')} />
            </FormField>
          </div>
          <FormField id="primaryDomain" label={t('tenants.domain')} error={errors.primaryDomain?.message}>
            <Input
              placeholder="acme.example.com"
              {...fieldProps('primaryDomain', errors.primaryDomain?.message)}
              {...form.register('primaryDomain')}
            />
          </FormField>
          <div className="grid gap-4 sm:grid-cols-2">
            <FormField id="isolation" label={t('tenants.isolation')}>
              <NativeSelect id="isolation" {...form.register('isolation')}>
                <option value="Shared">Shared</option>
                <option value="Dedicated">Dedicated</option>
              </NativeSelect>
            </FormField>
            <FormField id="plan" label={t('tenants.plan')}>
              <Input id="plan" {...form.register('plan')} />
            </FormField>
          </div>
          {dedicated && (
            <div className="grid gap-4 sm:grid-cols-2">
              <FormField id="databaseProvider" label={t('tenants.provider')} error={errors.databaseProvider?.message}>
                <NativeSelect
                  {...fieldProps('databaseProvider', errors.databaseProvider?.message)}
                  {...form.register('databaseProvider')}
                >
                  <option value="" />
                  <option value="PostgreSql">PostgreSql</option>
                  <option value="SqlServer">SqlServer</option>
                  <option value="MySql">MySql</option>
                </NativeSelect>
              </FormField>
              <FormField
                id="connectionSecretRef"
                label={t('tenants.secretRef')}
                hint={t('tenants.secretHint')}
                error={errors.connectionSecretRef?.message}
              >
                <Input
                  {...fieldProps('connectionSecretRef', errors.connectionSecretRef?.message)}
                  {...form.register('connectionSecretRef')}
                />
              </FormField>
            </div>
          )}
          <DialogFooter>
            <Button variant="outline" onClick={() => onOpenChange(false)}>
              {t('common:actions.cancel')}
            </Button>
            <Button type="submit" disabled={save.isPending}>
              {t('common:actions.create')}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function ManageTenantDialog({ tenant, onClose }: { tenant: Tenant | null; onClose: () => void }) {
  const { t } = useTranslation('tenancy');
  const scope = useQueryScope('tenancy', 'tenants');
  const client = useQueryClient();
  const [current, setCurrent] = useState<Tenant | null>(tenant);
  const [host, setHost] = useState('');
  const [feature, setFeature] = useState('');

  useEffect(() => setCurrent(tenant), [tenant]);

  const run = useMutation({
    mutationFn: (action: () => Promise<Tenant>) => action(),
    onSuccess: async (updated) => {
      setCurrent(updated);
      await client.invalidateQueries({ queryKey: scope });
    },
    onError: (error) => toast.error(ApiError.from(error).message),
  });

  if (!current) {
    return null;
  }

  return (
    <Dialog open={Boolean(tenant)} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-xl">
        <DialogHeader>
          <DialogTitle>{current.name}</DialogTitle>
          <DialogDescription>
            {current.code} · {t(`tenants.statuses.${current.status}`)} · {current.isolation}
          </DialogDescription>
        </DialogHeader>

        <div className="flex flex-wrap gap-2">
          {current.status === 'Active' ? (
            <Button
              variant="outline"
              size="sm"
              onClick={() => run.mutate(() => tenancyApi.setStatus(current.id, 'Suspended'))}
            >
              {t('tenants.suspend')}
            </Button>
          ) : current.status === 'Suspended' ? (
            <Button
              variant="outline"
              size="sm"
              onClick={() => run.mutate(() => tenancyApi.setStatus(current.id, 'Active'))}
            >
              {t('tenants.activate')}
            </Button>
          ) : null}
          <Button
            variant="outline"
            size="sm"
            onClick={() =>
              run.mutate(() => tenancyApi.provision(current.id), {
                onSuccess: () => toast.success(t('tenants.provisioned')),
              })
            }
          >
            <DatabaseIcon />
            {t('tenants.provision')}
          </Button>
        </div>

        <section className="space-y-2">
          <Label>{t('tenants.domains')}</Label>
          <ul className="divide-y rounded-md border">
            {current.domains.map((domain) => (
              <li key={domain.id} className="flex items-center justify-between px-3 py-1.5 text-sm">
                <span className="font-mono">{domain.host}</span>
                <span className="flex items-center gap-2">
                  {domain.isPrimary && <Badge variant="secondary">primary</Badge>}
                  <Button
                    variant="ghost"
                    size="icon"
                    className="size-7"
                    aria-label={t('common:actions.delete')}
                    onClick={() => run.mutate(() => tenancyApi.removeDomain(current.id, domain.id))}
                  >
                    <XIcon />
                  </Button>
                </span>
              </li>
            ))}
          </ul>
          <div className="flex gap-2">
            <Input
              aria-label={t('tenants.addDomain')}
              placeholder="erp.example.com"
              value={host}
              onChange={(event) => setHost(event.target.value)}
            />
            <Button
              disabled={!host}
              onClick={() => run.mutate(() => tenancyApi.addDomain(current.id, host), { onSuccess: () => setHost('') })}
            >
              {t('tenants.addDomain')}
            </Button>
          </div>
        </section>

        <section className="space-y-2">
          <Label>{t('tenants.features')}</Label>
          {current.features.map((item) => (
            <Label key={item.feature} className="justify-between font-normal">
              <span className="font-mono">{item.feature}</span>
              <Switch
                checked={item.enabled}
                onCheckedChange={(enabled) =>
                  run.mutate(() => tenancyApi.setFeature(current.id, item.feature, enabled))
                }
              />
            </Label>
          ))}
          <div className="flex gap-2">
            <Input
              aria-label={t('tenants.featureName')}
              placeholder="catalog.advanced-pricing"
              value={feature}
              onChange={(event) => setFeature(event.target.value)}
            />
            <Button
              disabled={!feature}
              onClick={() =>
                run.mutate(() => tenancyApi.setFeature(current.id, feature, true), { onSuccess: () => setFeature('') })
              }
            >
              <PlusIcon />
            </Button>
          </div>
        </section>

        <DialogFooter>
          <Button onClick={onClose}>{t('common:actions.close')}</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

export function TenantsPage() {
  const { t } = useTranslation('tenancy');
  const scope = useQueryScope('tenancy', 'tenants');
  const [state, setState] = useTableState({ sort: 'name' });
  const [creating, setCreating] = useState(false);
  const [managing, setManaging] = useState<Tenant | null>(null);
  const canManage = usePermission('tenancy.tenants.manage');
  const list = useCrudList(
    scope,
    { list: tenancyApi.list },
    toListParams(state, (q) => `name:contains:${q}`),
  );

  const columns = useMemo<ColumnDef<Tenant, unknown>[]>(
    () => [
      { accessorKey: 'code', header: t('tenants.code'), meta: { sortField: 'code', className: 'font-mono' } },
      { accessorKey: 'name', header: t('common:common.name'), meta: { sortField: 'name' } },
      {
        accessorKey: 'status',
        header: t('common:common.status'),
        meta: { sortField: 'status' },
        cell: ({ row }) => (
          <Badge variant={row.original.status === 'Active' ? 'success' : 'outline'}>
            {t(`tenants.statuses.${row.original.status}`)}
          </Badge>
        ),
      },
      { accessorKey: 'isolation', header: t('tenants.isolation') },
      {
        id: 'domains',
        header: t('tenants.domains'),
        cell: ({ row }) => row.original.domains.map((domain) => domain.host).join(', '),
      },
      {
        accessorKey: 'createdAt',
        header: t('common:common.createdAt'),
        meta: { sortField: 'createdAt' },
        cell: ({ row }) => formatDate(row.original.createdAt),
      },
      {
        id: 'actions',
        enableHiding: false,
        header: () => <span className="sr-only">{t('common:actions.actions')}</span>,
        meta: { className: 'w-12 text-right' },
        cell: ({ row }) =>
          canManage && (
            <Button
              variant="ghost"
              size="icon"
              className="size-8"
              aria-label={t('common:actions.edit')}
              onClick={() => setManaging(row.original)}
            >
              <SlidersHorizontalIcon />
            </Button>
          ),
      },
    ],
    [t, canManage],
  );

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('tenants.title')}
        description={t('tenants.description')}
        actions={
          <Can permission="tenancy.tenants.manage">
            <Button onClick={() => setCreating(true)}>
              <PlusIcon />
              {t('tenants.new')}
            </Button>
          </Can>
        }
      />
      <DataTable
        id="tenancy-tenants"
        columns={columns}
        data={list.data}
        isLoading={list.isLoading}
        error={list.error}
        onRetry={() => void list.refetch()}
        state={state}
        onStateChange={setState}
      />
      <CreateTenantDialog open={creating} onOpenChange={setCreating} />
      <ManageTenantDialog tenant={managing} onClose={() => setManaging(null)} />
    </div>
  );
}
