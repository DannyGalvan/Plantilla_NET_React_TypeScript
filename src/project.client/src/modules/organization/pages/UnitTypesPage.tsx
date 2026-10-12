import { zodResolver } from '@hookform/resolvers/zod';
import type { ColumnDef } from '@tanstack/react-table';
import { PencilIcon, PlusIcon, Trash2Icon } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { z } from 'zod';

import { useCrudList, useCrudMutations } from '@/shared/api/crud-api';
import { ApiError } from '@/shared/api/problem-details';
import { useQueryScope } from '@/shared/api/query-keys';
import { Can, usePermission } from '@/shared/auth/permissions';
import { ConfirmDialog } from '@/shared/components/confirm-dialog';
import { DataTable, toListParams, useTableState } from '@/shared/components/data-table';
import { applyServerErrors, fieldProps, FormField } from '@/shared/components/form';
import { PageHeader } from '@/shared/components/page';
import {
  Badge,
  Button,
  Checkbox,
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Input,
  Label,
} from '@/shared/ui';

import { unitTypesApi, type UnitType } from '../api';

const formSchema = z.object({
  code: z.string().trim().min(1).max(32),
  name: z.string().trim().min(1).max(64),
  canBeRoot: z.boolean(),
  allowedParentCodes: z.string().max(256),
  sortOrder: z.coerce.number<number>().int(),
});
type TypeForm = z.infer<typeof formSchema>;

export function UnitTypesPage() {
  const { t } = useTranslation('organization');
  const scope = useQueryScope('organization', 'types');
  const [state, setState] = useTableState({ sort: 'sortOrder' });
  const [editing, setEditing] = useState<UnitType | null>(null);
  const [open, setOpen] = useState(false);
  const [deleting, setDeleting] = useState<UnitType | null>(null);
  const canUpdate = usePermission('organization.unit-types.update');
  const canDelete = usePermission('organization.unit-types.delete');
  const list = useCrudList(
    scope,
    unitTypesApi,
    toListParams(state, (q) => `name:contains:${q}`),
  );
  const { create, update, remove } = useCrudMutations(scope, unitTypesApi);
  const form = useForm<TypeForm>({ resolver: zodResolver(formSchema) });
  const errors = form.formState.errors;

  useEffect(() => {
    if (open) {
      form.reset({
        code: editing?.code ?? '',
        name: editing?.name ?? '',
        canBeRoot: editing?.canBeRoot ?? false,
        allowedParentCodes: editing?.allowedParentCodes ?? '',
        sortOrder: editing?.sortOrder ?? 0,
      });
    }
  }, [open, editing, form]);

  const submit = form.handleSubmit(async (values) => {
    try {
      if (editing) {
        await update.mutateAsync({ id: editing.id, input: { ...values, concurrencyStamp: editing.concurrencyStamp } });
      } else {
        await create.mutateAsync(values);
      }

      toast.success(t('common:common.saved'));
      setOpen(false);
    } catch (error) {
      applyServerErrors(form, error);
    }
  });

  const columns = useMemo<ColumnDef<UnitType, unknown>[]>(
    () => [
      { accessorKey: 'code', header: t('common:common.code'), meta: { sortField: 'code', className: 'font-mono' } },
      { accessorKey: 'name', header: t('common:common.name'), meta: { sortField: 'name' } },
      {
        id: 'root',
        header: t('types.canBeRoot'),
        cell: ({ row }) =>
          row.original.canBeRoot ? <Badge variant="secondary">{t('common:common.yes')}</Badge> : null,
      },
      { accessorKey: 'allowedParentCodes', header: t('types.allowedParents') },
      { accessorKey: 'sortOrder', header: t('types.order'), meta: { sortField: 'sortOrder' } },
      {
        id: 'actions',
        enableHiding: false,
        header: () => <span className="sr-only">{t('common:actions.actions')}</span>,
        meta: { className: 'w-24 text-right' },
        cell: ({ row }) => (
          <div className="flex justify-end gap-1">
            {canUpdate && (
              <Button
                variant="ghost"
                size="icon"
                className="size-8"
                aria-label={t('common:actions.edit')}
                onClick={() => {
                  setEditing(row.original);
                  setOpen(true);
                }}
              >
                <PencilIcon />
              </Button>
            )}
            {canDelete && (
              <Button
                variant="ghost"
                size="icon"
                className="size-8 text-destructive"
                aria-label={t('common:actions.delete')}
                onClick={() => setDeleting(row.original)}
              >
                <Trash2Icon />
              </Button>
            )}
          </div>
        ),
      },
    ],
    [t, canUpdate, canDelete],
  );

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('types.title')}
        description={t('types.description')}
        actions={
          <Can permission="organization.unit-types.create">
            <Button
              onClick={() => {
                setEditing(null);
                setOpen(true);
              }}
            >
              <PlusIcon />
              {t('types.new')}
            </Button>
          </Can>
        }
      />
      <DataTable
        id="organization-types"
        columns={columns}
        data={list.data}
        isLoading={list.isLoading}
        error={list.error}
        onRetry={() => void list.refetch()}
        state={state}
        onStateChange={setState}
      />
      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{editing ? t('types.edit') : t('types.new')}</DialogTitle>
          </DialogHeader>
          <form className="grid gap-4" onSubmit={(event) => void submit(event)} noValidate>
            <div className="grid gap-4 sm:grid-cols-2">
              <FormField id="code" label={t('common:common.code')} error={errors.code?.message}>
                <Input
                  disabled={Boolean(editing)}
                  {...fieldProps('code', errors.code?.message)}
                  {...form.register('code')}
                />
              </FormField>
              <FormField id="sortOrder" label={t('types.order')} error={errors.sortOrder?.message}>
                <Input
                  type="number"
                  {...fieldProps('sortOrder', errors.sortOrder?.message)}
                  {...form.register('sortOrder')}
                />
              </FormField>
            </div>
            <FormField id="name" label={t('common:common.name')} error={errors.name?.message}>
              <Input {...fieldProps('name', errors.name?.message)} {...form.register('name')} />
            </FormField>
            <FormField
              id="allowedParentCodes"
              label={t('types.allowedParents')}
              error={errors.allowedParentCodes?.message}
            >
              <Input
                {...fieldProps('allowedParentCodes', errors.allowedParentCodes?.message)}
                {...form.register('allowedParentCodes')}
              />
            </FormField>
            <Label className="font-normal">
              <Checkbox
                checked={form.watch('canBeRoot')}
                onCheckedChange={(checked) => form.setValue('canBeRoot', Boolean(checked))}
              />
              {t('types.canBeRoot')}
            </Label>
            <DialogFooter>
              <Button variant="outline" onClick={() => setOpen(false)}>
                {t('common:actions.cancel')}
              </Button>
              <Button type="submit" disabled={form.formState.isSubmitting}>
                {t('common:actions.save')}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
      <ConfirmDialog
        open={Boolean(deleting)}
        onOpenChange={(value) => !value && setDeleting(null)}
        onConfirm={() =>
          deleting && remove.mutate(deleting.id, { onError: (error) => toast.error(ApiError.from(error).message) })
        }
      />
    </div>
  );
}
