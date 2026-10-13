import { useQuery } from '@tanstack/react-query';
import type { LucideIcon } from 'lucide-react';
import { Link } from 'react-router';

import { http } from '@/shared/api/http-client';
import { useQueryScope } from '@/shared/api/query-keys';
import { usePermission } from '@/shared/auth/permissions';
import { formatNumber } from '@/shared/lib/format';
import { Card, CardContent, CardHeader, CardTitle, Skeleton } from '@/shared/ui';

/** Total of a generic list endpoint (`pageSize=1` keeps the query cheap). */
export async function countOf(url: string, filter?: string): Promise<number> {
  const response = await http.get<{ total: number | null }>(url, { params: { pageSize: 1, filter } });
  return response.data.total ?? 0;
}

/** Numeric dashboard card linked to a page; hidden when the user lacks `permission`. */
export function KpiCard({
  id,
  title,
  icon: Icon,
  permission,
  to,
  load,
}: {
  id: string;
  title: string;
  icon: LucideIcon;
  permission: string;
  to: string;
  load: () => Promise<number>;
}) {
  const allowed = usePermission(permission);
  const scope = useQueryScope('dashboard');
  const value = useQuery({ queryKey: [...scope, id], queryFn: load, enabled: allowed });
  if (!allowed) {
    return null;
  }

  return (
    <Link to={to} className="rounded-xl focus-visible:ring-[3px] focus-visible:ring-ring/50 focus-visible:outline-none">
      <Card className="gap-2 transition-colors hover:bg-accent/40">
        <CardHeader className="flex flex-row items-center justify-between">
          <CardTitle className="text-sm font-medium text-muted-foreground">{title}</CardTitle>
          <Icon className="size-4 text-muted-foreground" aria-hidden />
        </CardHeader>
        <CardContent>
          {value.isLoading ? (
            <Skeleton className="h-8 w-16" />
          ) : (
            <p className="text-3xl font-semibold tabular-nums">{formatNumber(value.data ?? 0)}</p>
          )}
        </CardContent>
      </Card>
    </Link>
  );
}
