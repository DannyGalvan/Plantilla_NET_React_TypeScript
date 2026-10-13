import { keepPreviousData, useQuery } from '@tanstack/react-query';
import type { ColumnDef } from '@tanstack/react-table';
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { auditApi, type AuditEntry } from '@/shared/api/audit-api';
import { useQueryScope } from '@/shared/api/query-keys';
import { DataTable, useTableState } from '@/shared/components/data-table';
import { ChangeList } from '@/shared/components/history-panel';
import { PageHeader } from '@/shared/components/page';
import { formatDate } from '@/shared/lib/format';
import { Badge, Input } from '@/shared/ui';

/** Every change of every module in the tenant, newest first, filterable by record type and id. */
export function AuditPage() {
  const { t } = useTranslation('audit');
  const scope = useQueryScope('audit', 'entries');
  const [state, setState] = useTableState();
  const [entityType, setEntityType] = useState('');
  const [entityId, setEntityId] = useState('');
  const list = useQuery({
    queryKey: [...scope, state, entityType, entityId],
    queryFn: () =>
      auditApi.list({
        page: state.page,
        pageSize: state.pageSize,
        entityType: entityType || undefined,
        entityId: entityId || undefined,
      }),
    placeholderData: keepPreviousData,
  });

  const columns = useMemo<ColumnDef<AuditEntry, unknown>[]>(
    () => [
      { accessorKey: 'occurredAt', header: t('when'), cell: ({ row }) => formatDate(row.original.occurredAt) },
      { id: 'user', header: t('user'), cell: ({ row }) => row.original.userName ?? row.original.userId },
      {
        id: 'operation',
        header: t('operation'),
        cell: ({ row }) => (
          <Badge variant="secondary">{t(`common:history.operations.${row.original.operation}`)}</Badge>
        ),
      },
      {
        id: 'record',
        header: t('record'),
        cell: ({ row }) => (
          <span className="font-mono text-xs">
            {row.original.entityType} · {row.original.entityId}
          </span>
        ),
      },
      { id: 'changes', header: t('changes'), cell: ({ row }) => <ChangeList entry={row.original} /> },
    ],
    [t],
  );

  return (
    <div className="space-y-6">
      <PageHeader title={t('title')} description={t('description')} />
      <div className="flex flex-wrap gap-2">
        <Input
          className="max-w-56"
          placeholder={t('entityType')}
          aria-label={t('entityType')}
          value={entityType}
          onChange={(event) => {
            setEntityType(event.target.value.trim());
            setState({ page: 1 });
          }}
        />
        <Input
          className="max-w-80"
          placeholder={t('entityId')}
          aria-label={t('entityId')}
          value={entityId}
          onChange={(event) => {
            setEntityId(event.target.value.trim());
            setState({ page: 1 });
          }}
        />
      </div>
      <DataTable
        id="audit-entries"
        columns={columns}
        data={list.data}
        isLoading={list.isLoading}
        error={list.error}
        onRetry={() => void list.refetch()}
        state={state}
        onStateChange={setState}
      />
    </div>
  );
}
