import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { ColumnDef } from '@tanstack/react-table';
import { KeyRoundIcon, MoreHorizontalIcon, PencilIcon, PlusIcon, Trash2Icon } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { z } from 'zod';

import { useCrudList } from '@/shared/api/crud-api';
import { ApiError } from '@/shared/api/problem-details';
import { useQueryScope } from '@/shared/api/query-keys';
import { Can, usePermission } from '@/shared/auth/permissions';
import { ConfirmDialog } from '@/shared/components/confirm-dialog';
import { DataTable, toListParams, useTableState } from '@/shared/components/data-table';
import { applyServerErrors, fieldProps, FormField } from '@/shared/components/form';
import { PageHeader, Spinner } from '@/shared/components/page';
import {
  Badge,
  Button,
  Checkbox,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  Input,
  Label,
  Textarea,
} from '@/shared/ui';

import { identityApi, type Role } from '../api';

const roleFormSchema = z.object({
  name: z.string().trim().min(1).max(64),
  description: z.string().max(256).optional(),
});
type RoleForm = z.infer<typeof roleFormSchema>;

function RoleDialog({
  role,
  open,
  onOpenChange,
}: {
  role: Role | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}) {
  const { t } = useTranslation('identity');
  const scope = useQueryScope('identity', 'roles');
  const client = useQueryClient();
  const form = useForm<RoleForm>({ resolver: zodResolver(roleFormSchema) });
  const errors = form.formState.errors;

  useEffect(() => {
    if (open) {
      form.reset({ name: role?.name ?? '', description: role?.description ?? '' });
    }
  }, [open, role, form]);

  const save = useMutation({
    mutationFn: (values: RoleForm) =>
      role
        ? identityApi.roles.update(role.id, { ...values, concurrencyStamp: role.concurrencyStamp })
        : identityApi.roles.create(values),
    onSuccess: async () => {
      await client.invalidateQueries({ queryKey: scope });
      toast.success(t('common:common.saved'));
      onOpenChange(false);
    },
    onError: (error) => applyServerErrors(form, error),
  });

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{role ? t('roles.edit') : t('roles.new')}</DialogTitle>
        </DialogHeader>
        <form
          className="grid gap-4"
          onSubmit={(event) => void form.handleSubmit((values) => save.mutate(values))(event)}
          noValidate
        >
          <FormField id="name" label={t('roles.name')} error={errors.name?.message}>
            <Input {...fieldProps('name', errors.name?.message)} {...form.register('name')} />
          </FormField>
          <FormField id="description" label={t('roles.description_field')} error={errors.description?.message}>
            <Textarea {...fieldProps('description', errors.description?.message)} {...form.register('description')} />
          </FormField>
          <DialogFooter>
            <Button variant="outline" onClick={() => onOpenChange(false)}>
              {t('common:actions.cancel')}
            </Button>
            <Button type="submit" disabled={save.isPending}>
              {t('common:actions.save')}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

/** Permission tree grouped by module, generated from the permissions the backend modules declare. */
function PermissionsDialog({ role, onOpenChange }: { role: Role | null; onOpenChange: (open: boolean) => void }) {
  const { t } = useTranslation('identity');
  const scope = useQueryScope('identity', 'roles');
  const client = useQueryClient();
  const groups = useQuery({
    queryKey: [...scope, 'permissions'],
    queryFn: identityApi.permissions,
    enabled: Boolean(role),
  });
  const [selected, setSelected] = useState<Set<string>>(new Set());

  useEffect(() => {
    setSelected(new Set(role?.permissions ?? []));
  }, [role]);

  const save = useMutation({
    mutationFn: () => identityApi.roles.setPermissions(role!.id, [...selected]),
    onSuccess: async () => {
      await client.invalidateQueries({ queryKey: scope });
      toast.success(t('roles.permissionsSaved'));
      onOpenChange(false);
    },
    onError: (error) => toast.error(ApiError.from(error).message),
  });

  const toggle = (keys: string[], checked: boolean) =>
    setSelected((current) => {
      const next = new Set(current);
      keys.forEach((key) => (checked ? next.add(key) : next.delete(key)));
      return next;
    });

  return (
    <Dialog open={Boolean(role)} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle>{t('roles.editPermissions')}</DialogTitle>
          <DialogDescription>{role?.name}</DialogDescription>
        </DialogHeader>
        {groups.isLoading ? (
          <Spinner />
        ) : (
          <div className="grid gap-4 sm:grid-cols-2">
            {groups.data?.map((group) => {
              const keys = group.permissions.map((permission) => permission.key);
              const all = keys.every((key) => selected.has(key));
              return (
                <fieldset key={group.module} className="rounded-lg border p-3">
                  <legend className="px-1 text-sm font-semibold capitalize">{group.module}</legend>
                  <Label className="mb-2 font-normal text-muted-foreground">
                    <Checkbox checked={all} onCheckedChange={(checked) => toggle(keys, Boolean(checked))} />
                    {t('roles.selectAll')}
                  </Label>
                  <div className="grid gap-1.5">
                    {group.permissions.map((permission) => (
                      <Label key={permission.key} className="font-mono text-xs font-normal">
                        <Checkbox
                          checked={selected.has(permission.key)}
                          onCheckedChange={(checked) => toggle([permission.key], Boolean(checked))}
                        />
                        {permission.key.split('.').slice(1).join('.')}
                      </Label>
                    ))}
                  </div>
                </fieldset>
              );
            })}
          </div>
        )}
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            {t('common:actions.cancel')}
          </Button>
          <Button onClick={() => save.mutate()} disabled={save.isPending}>
            {t('common:actions.save')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

export function RolesPage() {
  const { t } = useTranslation('identity');
  const scope = useQueryScope('identity', 'roles');
  const client = useQueryClient();
  const [state, setState] = useTableState({ sort: 'name' });
  const [editing, setEditing] = useState<Role | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [permissionsOf, setPermissionsOf] = useState<Role | null>(null);
  const [deleting, setDeleting] = useState<Role | null>(null);
  const canUpdate = usePermission('identity.roles.update');
  const canAssign = usePermission('identity.roles.assign-permissions');
  const canDelete = usePermission('identity.roles.delete');

  const roles = useCrudList(
    scope,
    identityApi.roles,
    toListParams(state, (q) => `name:contains:${q}`),
  );

  const remove = useMutation({
    mutationFn: (role: Role) => identityApi.roles.remove(role.id),
    onSuccess: async () => {
      await client.invalidateQueries({ queryKey: scope });
      toast.success(t('common:common.deleted'));
    },
    onError: (error) => toast.error(ApiError.from(error).message),
  });

  const columns = useMemo<ColumnDef<Role, unknown>[]>(
    () => [
      {
        accessorKey: 'name',
        header: t('roles.name'),
        meta: { sortField: 'name' },
        cell: ({ row }) => (
          <div className="flex items-center gap-2">
            <span className="font-medium">{row.original.name}</span>
            {row.original.isSystem && <Badge variant="outline">{t('roles.system')}</Badge>}
            {row.original.isPlatform && !row.original.isSystem && (
              <Badge variant="outline">{t('roles.platform')}</Badge>
            )}
          </div>
        ),
      },
      { accessorKey: 'description', header: t('roles.description_field') },
      { id: 'permissions', header: t('roles.permissions'), cell: ({ row }) => row.original.permissions.length },
      { accessorKey: 'userCount', header: t('roles.users') },
      {
        id: 'actions',
        header: () => <span className="sr-only">{t('common:actions.actions')}</span>,
        enableHiding: false,
        meta: { className: 'w-12 text-right' },
        cell: ({ row }) => {
          const editable = !row.original.isSystem && !row.original.isPlatform;
          if (!editable || !(canUpdate || canAssign || canDelete)) {
            return null;
          }

          return (
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" size="icon" className="size-8" aria-label={t('common:actions.actions')}>
                  <MoreHorizontalIcon />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                {canUpdate && (
                  <DropdownMenuItem
                    onSelect={() => {
                      setEditing(row.original);
                      setDialogOpen(true);
                    }}
                  >
                    <PencilIcon />
                    {t('common:actions.edit')}
                  </DropdownMenuItem>
                )}
                {canAssign && (
                  <DropdownMenuItem onSelect={() => setPermissionsOf(row.original)}>
                    <KeyRoundIcon />
                    {t('roles.editPermissions')}
                  </DropdownMenuItem>
                )}
                {canDelete && (
                  <DropdownMenuItem variant="destructive" onSelect={() => setDeleting(row.original)}>
                    <Trash2Icon />
                    {t('common:actions.delete')}
                  </DropdownMenuItem>
                )}
              </DropdownMenuContent>
            </DropdownMenu>
          );
        },
      },
    ],
    [t, canUpdate, canAssign, canDelete],
  );

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('roles.title')}
        description={t('roles.description')}
        actions={
          <Can permission="identity.roles.create">
            <Button
              onClick={() => {
                setEditing(null);
                setDialogOpen(true);
              }}
            >
              <PlusIcon />
              {t('roles.new')}
            </Button>
          </Can>
        }
      />
      <DataTable
        id="identity-roles"
        columns={columns}
        data={roles.data}
        isLoading={roles.isLoading}
        error={roles.error}
        onRetry={() => void roles.refetch()}
        state={state}
        onStateChange={setState}
      />
      <RoleDialog role={editing} open={dialogOpen} onOpenChange={setDialogOpen} />
      <PermissionsDialog role={permissionsOf} onOpenChange={(open) => !open && setPermissionsOf(null)} />
      <ConfirmDialog
        open={Boolean(deleting)}
        onOpenChange={(open) => !open && setDeleting(null)}
        onConfirm={() => deleting && remove.mutate(deleting)}
      />
    </div>
  );
}
