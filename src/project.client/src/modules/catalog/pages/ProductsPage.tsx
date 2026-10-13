import { zodResolver } from '@hookform/resolvers/zod';
import { useQuery } from '@tanstack/react-query';
import type { ColumnDef } from '@tanstack/react-table';
import { PencilIcon, PlusIcon, Trash2Icon } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router';
import { toast } from 'sonner';
import { z } from 'zod';

import { useCrudList, useCrudMutations } from '@/shared/api/crud-api';
import { ApiError } from '@/shared/api/problem-details';
import { useQueryScope } from '@/shared/api/query-keys';
import { Can, usePermission } from '@/shared/auth/permissions';
import { Attachments } from '@/shared/components/attachments';
import { ConfirmDialog } from '@/shared/components/confirm-dialog';
import { DataTable, toListParams, useTableState } from '@/shared/components/data-table';
import { applyServerErrors, fieldProps, FormField } from '@/shared/components/form';
import { PageHeader } from '@/shared/components/page';
import { formatCurrency, formatNumber } from '@/shared/lib/format';
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
  NativeSelect,
  Textarea,
} from '@/shared/ui';

import { categoriesApi, productsApi, type Product } from '../api';

const formSchema = z.object({
  sku: z.string().trim().min(1).max(32),
  name: z.string().trim().min(1).max(128),
  description: z.string().max(1024).optional(),
  price: z.coerce.number<number>().min(0),
  stock: z.coerce.number<number>().int().min(0),
  categoryId: z.string().min(1),
  isActive: z.boolean(),
});
type ProductForm = z.infer<typeof formSchema>;

export function ProductsPage() {
  const { t } = useTranslation('catalog');
  const scope = useQueryScope('catalog', 'products');
  const [state, setState] = useTableState({ sort: 'name' });
  const [params, setParams] = useSearchParams();
  const categoryFilter = params.get('category') ?? '';
  const [editing, setEditing] = useState<Product | null>(null);
  const [open, setOpen] = useState(false);
  const [deleting, setDeleting] = useState<Product | null>(null);
  const canUpdate = usePermission('catalog.products.update');
  const canDelete = usePermission('catalog.products.delete');

  const categoryScope = useQueryScope('catalog', 'categories');
  const categories = useQuery({
    queryKey: [...categoryScope, 'options'],
    queryFn: async () => (await categoriesApi.list({ pageSize: 100, sort: 'name' })).items,
  });
  const categoryName = (id: string) => categories.data?.find((category) => category.id === id)?.name ?? '';

  const list = useCrudList(
    scope,
    productsApi,
    toListParams(state, (q) => `name:contains:${q}`, categoryFilter ? `categoryId:eq:${categoryFilter}` : undefined),
  );
  const { create, update, remove } = useCrudMutations(scope, productsApi);
  const form = useForm<ProductForm>({ resolver: zodResolver(formSchema) });
  const errors = form.formState.errors;

  useEffect(() => {
    if (open) {
      form.reset({
        sku: editing?.sku ?? '',
        name: editing?.name ?? '',
        description: editing?.description ?? '',
        price: editing?.price ?? 0,
        stock: editing?.stock ?? 0,
        categoryId: editing?.categoryId ?? categoryFilter,
        isActive: editing?.isActive ?? true,
      });
    }
  }, [open, editing, form, categoryFilter]);

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

  const columns = useMemo<ColumnDef<Product, unknown>[]>(
    () => [
      { accessorKey: 'sku', header: t('products.sku'), meta: { sortField: 'sku', className: 'font-mono' } },
      { accessorKey: 'name', header: t('common:common.name'), meta: { sortField: 'name' } },
      { id: 'category', header: t('products.category'), cell: ({ row }) => categoryName(row.original.categoryId) },
      {
        accessorKey: 'price',
        header: t('products.price'),
        meta: { sortField: 'price', className: 'text-right' },
        cell: ({ row }) => formatCurrency(row.original.price),
      },
      {
        accessorKey: 'stock',
        header: t('products.stock'),
        meta: { sortField: 'stock', className: 'text-right' },
        cell: ({ row }) =>
          row.original.stock < 5 ? (
            <Badge variant="warning">{formatNumber(row.original.stock)}</Badge>
          ) : (
            formatNumber(row.original.stock)
          ),
      },
      {
        id: 'status',
        header: t('common:common.status'),
        cell: ({ row }) =>
          row.original.isActive ? (
            <Badge variant="success">{t('common:common.active')}</Badge>
          ) : (
            <Badge variant="outline">{t('common:common.inactive')}</Badge>
          ),
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
    // eslint-disable-next-line react-hooks/exhaustive-deps -- categoryName reads the cached categories.
    [t, canUpdate, canDelete, categories.data],
  );

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('products.title')}
        description={t('products.description')}
        actions={
          <Can permission="catalog.products.create">
            <Button
              onClick={() => {
                setEditing(null);
                setOpen(true);
              }}
            >
              <PlusIcon />
              {t('products.new')}
            </Button>
          </Can>
        }
      />
      <DataTable
        id="catalog-products"
        columns={columns}
        data={list.data}
        isLoading={list.isLoading}
        error={list.error}
        onRetry={() => void list.refetch()}
        state={state}
        onStateChange={setState}
        toolbar={
          <NativeSelect
            aria-label={t('products.category')}
            className="h-8 w-48"
            value={categoryFilter}
            onChange={(event) =>
              setParams((current) => {
                const next = new URLSearchParams(current);
                if (event.target.value) {
                  next.set('category', event.target.value);
                } else {
                  next.delete('category');
                }

                next.delete('page');
                return next;
              })
            }
          >
            <option value="">{t('products.allCategories')}</option>
            {categories.data?.map((category) => (
              <option key={category.id} value={category.id}>
                {category.name}
              </option>
            ))}
          </NativeSelect>
        }
      />

      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{editing ? t('products.edit') : t('products.new')}</DialogTitle>
          </DialogHeader>
          <form className="grid gap-4" onSubmit={(event) => void submit(event)} noValidate>
            <div className="grid gap-4 sm:grid-cols-2">
              <FormField id="sku" label={t('products.sku')} error={errors.sku?.message}>
                <Input
                  disabled={Boolean(editing)}
                  {...fieldProps('sku', errors.sku?.message)}
                  {...form.register('sku')}
                />
              </FormField>
              <FormField id="categoryId" label={t('products.category')} error={errors.categoryId?.message}>
                <NativeSelect
                  {...fieldProps('categoryId', errors.categoryId?.message)}
                  {...form.register('categoryId')}
                >
                  <option value="">{t('products.selectCategory')}</option>
                  {categories.data?.map((category) => (
                    <option key={category.id} value={category.id}>
                      {category.name}
                    </option>
                  ))}
                </NativeSelect>
              </FormField>
            </div>
            <FormField id="name" label={t('common:common.name')} error={errors.name?.message}>
              <Input {...fieldProps('name', errors.name?.message)} {...form.register('name')} />
            </FormField>
            <FormField id="description" label={t('common:common.description')} error={errors.description?.message}>
              <Textarea {...fieldProps('description', errors.description?.message)} {...form.register('description')} />
            </FormField>
            <div className="grid gap-4 sm:grid-cols-2">
              <FormField id="price" label={t('products.price')} error={errors.price?.message}>
                <Input
                  type="number"
                  step="0.01"
                  min="0"
                  {...fieldProps('price', errors.price?.message)}
                  {...form.register('price')}
                />
              </FormField>
              <FormField id="stock" label={t('products.stock')} error={errors.stock?.message}>
                <Input
                  type="number"
                  step="1"
                  min="0"
                  {...fieldProps('stock', errors.stock?.message)}
                  {...form.register('stock')}
                />
              </FormField>
            </div>
            <Label className="font-normal">
              <Checkbox
                checked={form.watch('isActive')}
                onCheckedChange={(checked) => form.setValue('isActive', Boolean(checked))}
              />
              {t('common:common.active')}
            </Label>
            {editing && <Attachments entityType="catalog.product" entityId={editing.id} canWrite={canUpdate} />}
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
