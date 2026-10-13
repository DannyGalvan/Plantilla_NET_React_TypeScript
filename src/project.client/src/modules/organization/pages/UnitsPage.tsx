import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { MoveIcon, PencilIcon, PlusIcon, Trash2Icon, UsersIcon, XIcon } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { z } from 'zod';

import { http } from '@/shared/api/http-client';
import { ApiError } from '@/shared/api/problem-details';
import { useQueryScope } from '@/shared/api/query-keys';
import { Can, usePermission } from '@/shared/auth/permissions';
import { ConfirmDialog } from '@/shared/components/confirm-dialog';
import { applyServerErrors, fieldProps, FormField } from '@/shared/components/form';
import { EmptyState, PageHeader, QueryError, Spinner } from '@/shared/components/page';
import {
  Badge,
  Button,
  Card,
  Checkbox,
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Input,
  Label,
  NativeSelect,
} from '@/shared/ui';

import { unitsApi, unitTypesApi, type Unit } from '../api';

const unitFormSchema = z.object({
  typeCode: z.string().min(1),
  parentId: z.string().nullable(),
  code: z.string().trim().min(1).max(32),
  name: z.string().trim().min(1).max(128),
  countryCode: z.string().max(2).optional(),
  isActive: z.boolean(),
});
type UnitForm = z.infer<typeof unitFormSchema>;

type DialogState =
  | { kind: 'create'; parentId: string | null }
  | { kind: 'edit'; unit: Unit }
  | { kind: 'move'; unit: Unit }
  | { kind: 'users'; unit: Unit }
  | null;

function UnitDialog({ state, units, onClose }: { state: DialogState; units: Unit[]; onClose: () => void }) {
  const { t } = useTranslation('organization');
  const scope = useQueryScope('organization');
  const client = useQueryClient();
  const types = useQuery({
    queryKey: [...scope, 'types-all'],
    queryFn: async () => (await unitTypesApi.list({ pageSize: 100, sort: 'sortOrder' })).items,
  });
  const form = useForm<UnitForm>({ resolver: zodResolver(unitFormSchema) });
  const errors = form.formState.errors;
  const open = state?.kind === 'create' || state?.kind === 'edit';
  const editing = state?.kind === 'edit' ? state.unit : null;

  useEffect(() => {
    if (state?.kind === 'create') {
      form.reset({ typeCode: '', parentId: state.parentId, code: '', name: '', countryCode: '', isActive: true });
    } else if (state?.kind === 'edit') {
      form.reset({ ...state.unit, countryCode: state.unit.countryCode ?? '' });
    }
  }, [state, form]);

  const save = useMutation({
    mutationFn: (values: UnitForm) =>
      editing
        ? unitsApi.update(editing.id, {
            code: values.code,
            name: values.name,
            countryCode: values.countryCode || undefined,
            isActive: values.isActive,
            concurrencyStamp: editing.concurrencyStamp,
          })
        : unitsApi.create({
            typeCode: values.typeCode,
            parentId: values.parentId,
            code: values.code,
            name: values.name,
            countryCode: values.countryCode || undefined,
          }),
    onSuccess: async () => {
      await client.invalidateQueries({ queryKey: scope });
      toast.success(t('common:common.saved'));
      onClose();
    },
    onError: (error) => applyServerErrors(form, error),
  });

  return (
    <Dialog open={open} onOpenChange={(value) => !value && onClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{editing ? t('units.edit') : t('units.new')}</DialogTitle>
        </DialogHeader>
        <form
          className="grid gap-4"
          onSubmit={(event) => void form.handleSubmit((values) => save.mutate(values))(event)}
          noValidate
        >
          {!editing && (
            <>
              <FormField id="parentId" label={t('units.parent')}>
                <NativeSelect
                  id="parentId"
                  value={form.watch('parentId') ?? ''}
                  onChange={(event) => form.setValue('parentId', event.target.value || null)}
                >
                  <option value="">{t('units.root')}</option>
                  {units.map((unit) => (
                    <option key={unit.id} value={unit.id}>{`${'  '.repeat(unit.depth)}${unit.name}`}</option>
                  ))}
                </NativeSelect>
              </FormField>
              <FormField id="typeCode" label={t('units.type')} error={errors.typeCode?.message}>
                <NativeSelect {...fieldProps('typeCode', errors.typeCode?.message)} {...form.register('typeCode')}>
                  <option value="" />
                  {types.data?.map((type) => (
                    <option key={type.id} value={type.code}>
                      {type.name}
                    </option>
                  ))}
                </NativeSelect>
              </FormField>
            </>
          )}
          <div className="grid gap-4 sm:grid-cols-2">
            <FormField id="code" label={t('common:common.code')} error={errors.code?.message}>
              <Input {...fieldProps('code', errors.code?.message)} {...form.register('code')} />
            </FormField>
            <FormField id="countryCode" label={t('units.country')} error={errors.countryCode?.message}>
              <Input
                maxLength={2}
                {...fieldProps('countryCode', errors.countryCode?.message)}
                {...form.register('countryCode')}
              />
            </FormField>
          </div>
          <FormField id="name" label={t('common:common.name')} error={errors.name?.message}>
            <Input {...fieldProps('name', errors.name?.message)} {...form.register('name')} />
          </FormField>
          {editing && (
            <Label className="font-normal">
              <Checkbox
                checked={form.watch('isActive')}
                onCheckedChange={(checked) => form.setValue('isActive', Boolean(checked))}
              />
              {t('common:common.active')}
            </Label>
          )}
          <DialogFooter>
            <Button variant="outline" onClick={onClose}>
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

function MoveDialog({ unit, units, onClose }: { unit: Unit | null; units: Unit[]; onClose: () => void }) {
  const { t } = useTranslation('organization');
  const scope = useQueryScope('organization');
  const client = useQueryClient();
  const [target, setTarget] = useState('');
  const move = useMutation({
    mutationFn: () => unitsApi.move(unit!.id, target || null),
    onSuccess: async () => {
      await client.invalidateQueries({ queryKey: scope });
      toast.success(t('common:common.saved'));
      onClose();
    },
    onError: (error) => toast.error(ApiError.from(error).message),
  });

  return (
    <Dialog open={Boolean(unit)} onOpenChange={(value) => !value && onClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>
            {t('units.move')}: {unit?.name}
          </DialogTitle>
        </DialogHeader>
        <FormField id="target" label={t('units.moveTo')}>
          <NativeSelect id="target" value={target} onChange={(event) => setTarget(event.target.value)}>
            <option value="">{t('units.root')}</option>
            {units
              .filter((candidate) => candidate.id !== unit?.id)
              .map((candidate) => (
                <option
                  key={candidate.id}
                  value={candidate.id}
                >{`${'  '.repeat(candidate.depth)}${candidate.name}`}</option>
              ))}
          </NativeSelect>
        </FormField>
        <DialogFooter>
          <Button variant="outline" onClick={onClose}>
            {t('common:actions.cancel')}
          </Button>
          <Button onClick={() => move.mutate()} disabled={move.isPending}>
            {t('units.move')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

const memberSchema = z.object({
  items: z.array(z.object({ id: z.string(), userName: z.string(), displayName: z.string().nullable() })),
});

function UsersDialog({ unit, onClose }: { unit: Unit | null; onClose: () => void }) {
  const { t } = useTranslation('organization');
  const scope = useQueryScope('organization', 'unit-users');
  const client = useQueryClient();
  const canManage = usePermission('organization.assignments.manage');
  const [userId, setUserId] = useState('');
  const assigned = useQuery({
    queryKey: [...scope, unit?.id],
    queryFn: () => unitsApi.users(unit!.id),
    enabled: Boolean(unit),
  });
  const candidates = useQuery({
    queryKey: [...scope, 'candidates'],
    queryFn: async () =>
      memberSchema.parse((await http.get('/identity/users', { params: { pageSize: 100, sort: 'userName' } })).data)
        .items,
    enabled: Boolean(unit) && canManage,
  });

  const change = useMutation({
    mutationFn: ({ add, id }: { add: boolean; id: string }) =>
      add ? unitsApi.assign(unit!.id, id) : unitsApi.unassign(unit!.id, id),
    onSuccess: async () => {
      setUserId('');
      await client.invalidateQueries({ queryKey: scope });
    },
    onError: (error) => toast.error(ApiError.from(error).message),
  });

  return (
    <Dialog open={Boolean(unit)} onOpenChange={(value) => !value && onClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>
            {t('units.users')}: {unit?.name}
          </DialogTitle>
        </DialogHeader>
        {assigned.isLoading ? (
          <Spinner />
        ) : assigned.data?.length === 0 ? (
          <p className="text-sm text-muted-foreground">{t('units.noUsers')}</p>
        ) : (
          <ul className="divide-y rounded-md border">
            {assigned.data?.map((user) => (
              <li key={user.id} className="flex items-center justify-between px-3 py-2 text-sm">
                <span>{user.displayName ?? user.userName}</span>
                {canManage && (
                  <Button
                    variant="ghost"
                    size="icon"
                    className="size-7"
                    aria-label={t('common:actions.delete')}
                    onClick={() => change.mutate({ add: false, id: user.id })}
                  >
                    <XIcon />
                  </Button>
                )}
              </li>
            ))}
          </ul>
        )}
        {canManage && (
          <div className="flex gap-2">
            <NativeSelect
              aria-label={t('units.userId')}
              value={userId}
              onChange={(event) => setUserId(event.target.value)}
            >
              <option value="">{t('units.selectUser')}</option>
              {candidates.data
                ?.filter((user) => !assigned.data?.some((member) => member.id === user.id))
                .map((user) => (
                  <option key={user.id} value={user.id}>
                    {user.displayName ?? user.userName}
                  </option>
                ))}
            </NativeSelect>
            <Button disabled={!userId || change.isPending} onClick={() => change.mutate({ add: true, id: userId })}>
              {t('units.assign')}
            </Button>
          </div>
        )}
      </DialogContent>
    </Dialog>
  );
}

export function UnitsPage() {
  const { t } = useTranslation('organization');
  const scope = useQueryScope('organization');
  const client = useQueryClient();
  const [dialog, setDialog] = useState<DialogState>(null);
  const [deleting, setDeleting] = useState<Unit | null>(null);
  const units = useQuery({ queryKey: [...scope, 'units'], queryFn: () => unitsApi.list() });
  const canCreate = usePermission('organization.units.create');
  const canUpdate = usePermission('organization.units.update');
  const canDelete = usePermission('organization.units.delete');

  const remove = useMutation({
    mutationFn: (unit: Unit) => unitsApi.remove(unit.id),
    onSuccess: async () => {
      await client.invalidateQueries({ queryKey: scope });
      toast.success(t('common:common.deleted'));
    },
    onError: (error) => toast.error(ApiError.from(error).message),
  });

  const list = units.data ?? [];

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('units.title')}
        description={t('units.description')}
        actions={
          <Can permission="organization.units.create">
            <Button onClick={() => setDialog({ kind: 'create', parentId: null })}>
              <PlusIcon />
              {t('units.new')}
            </Button>
          </Can>
        }
      />
      {units.isLoading ? (
        <Spinner />
      ) : units.error ? (
        <QueryError error={units.error} onRetry={() => void units.refetch()} />
      ) : list.length === 0 ? (
        <EmptyState title={t('common:table.noResults')} />
      ) : (
        <Card className="gap-0 py-0">
          <ul role="tree" aria-label={t('units.title')}>
            {list.map((unit) => (
              <li
                key={unit.id}
                role="treeitem"
                aria-level={unit.depth + 1}
                aria-selected={false}
                className="flex items-center gap-3 border-b px-4 py-2 last:border-0"
              >
                <div
                  className="flex min-w-0 flex-1 items-center gap-2"
                  style={{ paddingLeft: `${unit.depth * 1.5}rem` }}
                >
                  <span className="truncate font-medium">{unit.name}</span>
                  <Badge variant="outline">{unit.typeCode}</Badge>
                  <span className="font-mono text-xs text-muted-foreground">{unit.code}</span>
                  {unit.countryCode && <Badge variant="secondary">{unit.countryCode}</Badge>}
                  {!unit.isActive && <Badge variant="outline">{t('common:common.inactive')}</Badge>}
                </div>
                <div className="flex shrink-0 gap-1">
                  <Button
                    variant="ghost"
                    size="icon"
                    className="size-8"
                    aria-label={t('units.users')}
                    onClick={() => setDialog({ kind: 'users', unit })}
                  >
                    <UsersIcon />
                  </Button>
                  {canCreate && (
                    <Button
                      variant="ghost"
                      size="icon"
                      className="size-8"
                      aria-label={t('units.newChild')}
                      onClick={() => setDialog({ kind: 'create', parentId: unit.id })}
                    >
                      <PlusIcon />
                    </Button>
                  )}
                  {canUpdate && (
                    <>
                      <Button
                        variant="ghost"
                        size="icon"
                        className="size-8"
                        aria-label={t('common:actions.edit')}
                        onClick={() => setDialog({ kind: 'edit', unit })}
                      >
                        <PencilIcon />
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        className="size-8"
                        aria-label={t('units.move')}
                        onClick={() => setDialog({ kind: 'move', unit })}
                      >
                        <MoveIcon />
                      </Button>
                    </>
                  )}
                  {canDelete && (
                    <Button
                      variant="ghost"
                      size="icon"
                      className="size-8 text-destructive"
                      aria-label={t('common:actions.delete')}
                      onClick={() => setDeleting(unit)}
                    >
                      <Trash2Icon />
                    </Button>
                  )}
                </div>
              </li>
            ))}
          </ul>
        </Card>
      )}
      <UnitDialog state={dialog} units={list} onClose={() => setDialog(null)} />
      <MoveDialog unit={dialog?.kind === 'move' ? dialog.unit : null} units={list} onClose={() => setDialog(null)} />
      <UsersDialog unit={dialog?.kind === 'users' ? dialog.unit : null} onClose={() => setDialog(null)} />
      <ConfirmDialog
        open={Boolean(deleting)}
        onOpenChange={(open) => !open && setDeleting(null)}
        onConfirm={() => deleting && remove.mutate(deleting)}
      />
    </div>
  );
}
