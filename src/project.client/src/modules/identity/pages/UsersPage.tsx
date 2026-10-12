import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { ColumnDef } from '@tanstack/react-table';
import { LockOpenIcon, MoreHorizontalIcon, PencilIcon, PlusIcon, Trash2Icon } from 'lucide-react';
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
import { PageHeader } from '@/shared/components/page';
import { formatDateTime } from '@/shared/lib/format';
import {
  Badge,
  Button,
  Checkbox,
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  Input,
  Label,
} from '@/shared/ui';

import { identityApi, type User } from '../api';

const userFormSchema = z.object({
  userName: z.string().trim().min(3).max(64),
  email: z.email(),
  displayName: z.string().max(128).optional(),
  password: z.string(),
  roleIds: z.array(z.string()),
  mustChangePassword: z.boolean(),
  isActive: z.boolean(),
});

type UserForm = z.infer<typeof userFormSchema>;

function UserDialog({
  user,
  open,
  onOpenChange,
}: {
  user: User | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}) {
  const { t } = useTranslation('identity');
  const scope = useQueryScope('identity', 'users');
  const client = useQueryClient();
  const roles = useQuery({ queryKey: [...scope, 'roles-all'], queryFn: identityApi.roles.all, enabled: open });
  const form = useForm<UserForm>({ resolver: zodResolver(userFormSchema) });
  const errors = form.formState.errors;

  useEffect(() => {
    if (open) {
      form.reset({
        userName: user?.userName ?? '',
        email: user?.email ?? '',
        displayName: user?.displayName ?? '',
        password: '',
        roleIds: user?.roles.map((role) => role.id) ?? [],
        mustChangePassword: true,
        isActive: user?.isActive ?? true,
      });
    }
  }, [open, user, form]);

  const save = useMutation({
    mutationFn: (values: UserForm) =>
      user
        ? identityApi.users.update(user.id, {
            email: values.email,
            displayName: values.displayName,
            isActive: values.isActive,
            roleIds: values.roleIds,
            concurrencyStamp: user.concurrencyStamp,
          })
        : identityApi.users.create({ ...values }),
    onSuccess: async () => {
      await client.invalidateQueries({ queryKey: scope });
      toast.success(t('common:common.saved'));
      onOpenChange(false);
    },
    onError: (error) => applyServerErrors(form, error),
  });

  const selectedRoles = form.watch('roleIds') ?? [];
  const assignable = (roles.data ?? []).filter((role) => !role.isSystem);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{user ? t('users.edit') : t('users.new')}</DialogTitle>
        </DialogHeader>
        <form
          className="grid gap-4"
          onSubmit={(event) => void form.handleSubmit((values) => save.mutate(values))(event)}
          noValidate
        >
          <FormField id="userName" label={t('users.userName')} error={errors.userName?.message}>
            <Input
              disabled={Boolean(user)}
              {...fieldProps('userName', errors.userName?.message)}
              {...form.register('userName')}
            />
          </FormField>
          <FormField id="email" label={t('users.email')} error={errors.email?.message}>
            <Input type="email" {...fieldProps('email', errors.email?.message)} {...form.register('email')} />
          </FormField>
          <FormField id="displayName" label={t('users.displayName')} error={errors.displayName?.message}>
            <Input {...fieldProps('displayName', errors.displayName?.message)} {...form.register('displayName')} />
          </FormField>
          {!user && (
            <FormField id="password" label={t('users.password')} error={errors.password?.message}>
              <Input
                type="password"
                autoComplete="new-password"
                {...fieldProps('password', errors.password?.message)}
                {...form.register('password')}
              />
            </FormField>
          )}
          <fieldset className="grid gap-2">
            <legend className="mb-2 text-sm font-medium">{t('users.roles')}</legend>
            {assignable.map((role) => (
              <Label key={role.id} className="font-normal">
                <Checkbox
                  checked={selectedRoles.includes(role.id)}
                  onCheckedChange={(checked) =>
                    form.setValue(
                      'roleIds',
                      checked ? [...selectedRoles, role.id] : selectedRoles.filter((id) => id !== role.id),
                    )
                  }
                />
                {role.name}
              </Label>
            ))}
          </fieldset>
          {user ? (
            <Label className="font-normal">
              <Checkbox
                checked={form.watch('isActive')}
                onCheckedChange={(checked) => form.setValue('isActive', Boolean(checked))}
              />
              {t('users.active')}
            </Label>
          ) : (
            <Label className="font-normal">
              <Checkbox
                checked={form.watch('mustChangePassword')}
                onCheckedChange={(checked) => form.setValue('mustChangePassword', Boolean(checked))}
              />
              {t('users.mustChangePassword')}
            </Label>
          )}
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

export function UsersPage() {
  const { t } = useTranslation('identity');
  const scope = useQueryScope('identity', 'users');
  const client = useQueryClient();
  const [state, setState] = useTableState({ sort: 'userName' });
  const [editing, setEditing] = useState<User | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [deleting, setDeleting] = useState<User | null>(null);
  const canUpdate = usePermission('identity.users.update');
  const canDelete = usePermission('identity.users.delete');

  const params = toListParams(state, (q) => `userName:contains:${q}`);
  const users = useCrudList(scope, identityApi.users, params);

  const action = useMutation({
    mutationFn: async ({ kind, user }: { kind: 'delete' | 'unlock'; user: User }) =>
      kind === 'delete' ? identityApi.users.remove(user.id) : identityApi.users.unlock(user.id),
    onSuccess: async (_, { kind }) => {
      await client.invalidateQueries({ queryKey: scope });
      toast.success(kind === 'delete' ? t('common:common.deleted') : t('users.unlocked'));
    },
    onError: (error) => toast.error(ApiError.from(error).message),
  });

  const columns = useMemo<ColumnDef<User, unknown>[]>(
    () => [
      { accessorKey: 'userName', header: t('users.userName'), meta: { sortField: 'userName' } },
      { accessorKey: 'email', header: t('users.email'), meta: { sortField: 'email' } },
      {
        id: 'roles',
        header: t('users.roles'),
        cell: ({ row }) => (
          <div className="flex flex-wrap gap-1">
            {row.original.roles.map((role) => (
              <Badge key={role.id} variant="secondary">
                {role.name}
              </Badge>
            ))}
          </div>
        ),
      },
      {
        id: 'status',
        header: t('common:common.status'),
        cell: ({ row }) =>
          row.original.isLockedOut ? (
            <Badge variant="destructive">{t('users.locked')}</Badge>
          ) : row.original.isActive ? (
            <Badge variant="success">{t('common:common.active')}</Badge>
          ) : (
            <Badge variant="outline">{t('common:common.inactive')}</Badge>
          ),
      },
      {
        accessorKey: 'lastLoginAt',
        header: t('users.lastLogin'),
        meta: { sortField: 'lastLoginAt' },
        cell: ({ row }) => formatDateTime(row.original.lastLoginAt),
      },
      {
        id: 'actions',
        header: () => <span className="sr-only">{t('common:actions.actions')}</span>,
        enableHiding: false,
        meta: { className: 'w-12 text-right' },
        cell: ({ row }) =>
          (canUpdate || canDelete) && (
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
                {canUpdate && row.original.isLockedOut && (
                  <DropdownMenuItem onSelect={() => action.mutate({ kind: 'unlock', user: row.original })}>
                    <LockOpenIcon />
                    {t('users.unlock')}
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
          ),
      },
    ],
    [t, canUpdate, canDelete, action],
  );

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('users.title')}
        description={t('users.description')}
        actions={
          <Can permission="identity.users.create">
            <Button
              onClick={() => {
                setEditing(null);
                setDialogOpen(true);
              }}
            >
              <PlusIcon />
              {t('users.new')}
            </Button>
          </Can>
        }
      />
      <DataTable
        id="identity-users"
        columns={columns}
        data={users.data}
        isLoading={users.isLoading}
        error={users.error}
        onRetry={() => void users.refetch()}
        state={state}
        onStateChange={setState}
      />
      <UserDialog user={editing} open={dialogOpen} onOpenChange={setDialogOpen} />
      <ConfirmDialog
        open={Boolean(deleting)}
        onOpenChange={(open) => !open && setDeleting(null)}
        onConfirm={() => deleting && action.mutate({ kind: 'delete', user: deleting })}
      />
    </div>
  );
}
