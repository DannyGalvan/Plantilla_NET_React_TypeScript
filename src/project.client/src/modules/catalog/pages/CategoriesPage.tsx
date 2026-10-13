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
import { DataExchange } from '@/shared/components/data-exchange';
import { DataTable, toListParams, useTableState } from '@/shared/components/data-table';
import { applyServerErrors, fieldProps, FormField } from '@/shared/components/form';
import { PageHeader } from '@/shared/components/page';
import { formatDate } from '@/shared/lib/format';
import { Button, Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle, Input, Textarea } from '@/shared/ui';

import { categoriesApi, type Category } from '../api';

const formSchema = z.object({ name: z.string().trim().min(1).max(128), description: z.string().max(512).optional() });
type CategoryForm = z.infer<typeof formSchema>;

export function CategoriesPage() {
  const { t } = useTranslation('catalog');
  const scope = useQueryScope('catalog', 'categories');
  const [state, setState] = useTableState({ sort: 'name' });
  const [editing, setEditing] = useState<Category | null>(null);
  const [open, setOpen] = useState(false);
  const [deleting, setDeleting] = useState<Category | null>(null);
  const canUpdate = usePermission('catalog.categories.update');
  const canDelete = usePermission('catalog.categories.delete');

  const params = toListParams(state, (q) => `name:contains:${q}`);
  const list = useCrudList(scope, categoriesApi, params);
  const { create, update, remove } = useCrudMutations(scope, categoriesApi);
  const form = useForm<CategoryForm>({ resolver: zodResolver(formSchema) });
  const errors = form.formState.errors;

  useEffect(() => {
    if (open) {
      form.reset({ name: editing?.name ?? '', description: editing?.description ?? '' });
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

  const columns = useMemo<ColumnDef<Category, unknown>[]>(
    () => [
      { accessorKey: 'name', header: t('common:common.name'), meta: { sortField: 'name' } },
      { accessorKey: 'description', header: t('common:common.description') },
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
        title={t('categories.title')}
        description={t('categories.description')}
        actions={
          <>
            <DataExchange
              resource="/catalog/categories"
              params={params}
              permission="catalog.categories"
              onImported={() => void list.refetch()}
            />
            <Can permission="catalog.categories.create">
              <Button
                onClick={() => {
                  setEditing(null);
                  setOpen(true);
                }}
              >
                <PlusIcon />
                {t('categories.new')}
              </Button>
            </Can>
          </>
        }
      />
      <DataTable
        id="catalog-categories"
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
            <DialogTitle>{editing ? t('categories.edit') : t('categories.new')}</DialogTitle>
          </DialogHeader>
          <form className="grid gap-4" onSubmit={(event) => void submit(event)} noValidate>
            <FormField id="name" label={t('common:common.name')} error={errors.name?.message}>
              <Input {...fieldProps('name', errors.name?.message)} {...form.register('name')} />
            </FormField>
            <FormField id="description" label={t('common:common.description')} error={errors.description?.message}>
              <Textarea {...fieldProps('description', errors.description?.message)} {...form.register('description')} />
            </FormField>
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
          deleting &&
          remove.mutate(deleting.id, {
            onSuccess: () => toast.success(t('common:common.deleted')),
            onError: (error) => toast.error(ApiError.from(error).message),
          })
        }
      />
    </div>
  );
}
