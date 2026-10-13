import { useTranslation } from 'react-i18next';

import { useAuthStore } from '@/shared/auth/auth-store';
import { usePermission } from '@/shared/auth/permissions';
import { PageHeader } from '@/shared/components/page';
import { getActiveModules } from '@/shared/modules/active-modules';
import type { DashboardWidget } from '@/shared/modules/manifest';
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/ui';

function WidgetSlot({ widget }: { widget: DashboardWidget }) {
  const allowed = usePermission(widget.permission ?? '');
  if (widget.permission && !allowed) {
    return null;
  }

  const Widget = widget.component;
  return <Widget />;
}

/** Home page. It knows no business module: every active module contributes its own widgets. */
export function DashboardPage() {
  const { t } = useTranslation('dashboard');
  const profile = useAuthStore((state) => state.profile);
  const widgets = getActiveModules()
    .flatMap((module) => module.widgets ?? [])
    .sort((a, b) => (a.order ?? 0) - (b.order ?? 0));
  const kpis = widgets.filter((widget) => (widget.size ?? 'kpi') === 'kpi');
  const wide = widgets.filter((widget) => widget.size === 'wide');

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('welcome', { name: profile?.displayName ?? profile?.userName ?? '' })}
        description={profile?.tenant?.name}
      />
      {kpis.length > 0 && (
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          {kpis.map((widget) => (
            <WidgetSlot key={widget.id} widget={widget} />
          ))}
        </div>
      )}
      {wide.map((widget) => (
        <WidgetSlot key={widget.id} widget={widget} />
      ))}
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
