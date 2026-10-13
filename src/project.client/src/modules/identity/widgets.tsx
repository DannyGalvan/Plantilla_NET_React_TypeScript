import { UsersIcon } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { countOf, KpiCard } from '@/shared/components/kpi-card';

export function UsersKpi() {
  const { t } = useTranslation();
  return (
    <KpiCard
      id="identity.users"
      title={t('nav.users')}
      icon={UsersIcon}
      permission="identity.users.read"
      to="/identity/users"
      load={() => countOf('/identity/users')}
    />
  );
}
