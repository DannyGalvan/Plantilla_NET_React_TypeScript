import { useQuery } from '@tanstack/react-query';
import { HistoryIcon } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { auditApi, type AuditEntry } from '@/shared/api/audit-api';
import { useQueryScope } from '@/shared/api/query-keys';
import { usePermission } from '@/shared/auth/permissions';
import { formatDate } from '@/shared/lib/format';
import { getActiveModules } from '@/shared/modules/active-modules';
import { Badge } from '@/shared/ui';

/** One change as text: "Name: Old -> New". */
export function ChangeList({ entry }: { entry: AuditEntry }) {
  return (
    <ul className="space-y-0.5 text-xs text-muted-foreground">
      {entry.changes.map((change) => (
        <li key={change.property} className="break-words">
          <span className="font-medium text-foreground">{change.property}</span>:{' '}
          {change.oldValue !== null && <span className="line-through">{change.oldValue}</span>}
          {change.oldValue !== null && ' → '}
          {change.newValue ?? '∅'}
        </li>
      ))}
    </ul>
  );
}

/**
 * Change history of one record, collapsed by default:
 * `<HistoryPanel entityType="catalog.product" entityId={product.id} />`. Hidden when the Audit module is not active
 * or the user cannot view the history.
 */
export function HistoryPanel({ entityType, entityId }: { entityType: string; entityId: string }) {
  const { t } = useTranslation();
  const allowed = usePermission('audit.history.view');
  const enabled = allowed && getActiveModules().some((module) => module.id === 'audit');
  const scope = useQueryScope('audit', entityType, entityId);
  const history = useQuery({
    queryKey: scope,
    queryFn: () => auditApi.list({ entityType, entityId, pageSize: 50 }),
    enabled,
  });

  if (!enabled) {
    return null;
  }

  return (
    <details className="rounded-md border px-3 py-2 text-sm">
      <summary className="flex cursor-pointer items-center gap-2 font-medium">
        <HistoryIcon className="size-4" aria-hidden />
        {t('history.title')}
      </summary>
      <ol className="mt-3 space-y-3">
        {history.data?.items.map((entry) => (
          <li key={entry.id} className="space-y-1">
            <div className="flex flex-wrap items-center gap-2 text-xs">
              <Badge variant="secondary">{t(`history.operations.${entry.operation}`)}</Badge>
              <span>{entry.userName ?? entry.userId}</span>
              <span className="text-muted-foreground">{formatDate(entry.occurredAt)}</span>
            </div>
            <ChangeList entry={entry} />
          </li>
        ))}
        {history.data?.items.length === 0 && <li className="text-muted-foreground">{t('history.empty')}</li>}
      </ol>
    </details>
  );
}
