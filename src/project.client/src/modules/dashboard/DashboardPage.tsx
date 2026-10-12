import { useQuery } from '@tanstack/react-query';
import { PackageIcon, TagsIcon, TriangleAlertIcon, UsersIcon, type LucideIcon } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';

import { http } from '@/shared/api/http-client';
import { useQueryScope } from '@/shared/api/query-keys';
import { useAuthStore } from '@/shared/auth/auth-store';
import { usePermission } from '@/shared/auth/permissions';
import { PageHeader } from '@/shared/components/page';
import { formatNumber } from '@/shared/lib/format';
import { Card, CardContent, CardHeader, CardTitle, Skeleton } from '@/shared/ui';

/** Total of a list endpoint (pageSize=1 keeps the query cheap). */
async function total(url: string, filter?: string): Promise<number> {
  const response = await http.get<{ total: number | null }>(url, { params: { pageSize: 1, filter } });
  return response.data.total ?? 0;
}

function Kpi({
  title,
  icon: Icon,
  permission,
  to,
  load,
  queryKey,
}: {
  title: string;
  icon: LucideIcon;
  permission: string;
  to: string;
  load: () => Promise<number>;
  queryKey: unknown[];
}) {
  const allowed = usePermission(permission);
  const value = useQuery({ queryKey, queryFn: load, enabled: allowed });
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

export function DashboardPage() {
  const { t } = useTranslation('dashboard');
  const scope = useQueryScope('dashboard');
  const profile = useAuthStore((state) => state.profile);

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('welcome', { name: profile?.displayName ?? profile?.userName ?? '' })}
        description={profile?.tenant?.name}
      />
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <Kpi
          title={t('common:nav.products')}
          icon={PackageIcon}
          permission="catalog.products.read"
          to="/catalog/products"
          queryKey={[...scope, 'products']}
          load={() => total('/catalog/products')}
        />
        <Kpi
          title={t('common:nav.categories')}
          icon={TagsIcon}
          permission="catalog.categories.read"
          to="/catalog/categories"
          queryKey={[...scope, 'categories']}
          load={() => total('/catalog/categories')}
        />
        <Kpi
          title={t('lowStock')}
          icon={TriangleAlertIcon}
          permission="catalog.products.read"
          to="/catalog/products?sort=stock"
          queryKey={[...scope, 'low']}
          load={() => total('/catalog/products', 'stock:lt:5')}
        />
        <Kpi
          title={t('common:nav.users')}
          icon={UsersIcon}
          permission="identity.users.read"
          to="/identity/users"
          queryKey={[...scope, 'users']}
          load={() => total('/identity/users')}
        />
      </div>
      <Card>
        <CardHeader>
          <CardTitle>{t('gettingStarted')}</CardTitle>
        </CardHeader>
        <CardContent className="space-y-2 text-sm text-muted-foreground">
          <p>{t('hint1')}</p>
          <p>{t('hint2')}</p>
        </CardContent>
      </Card>
    </div>
  );
}
