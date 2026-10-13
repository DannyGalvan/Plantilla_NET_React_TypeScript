import { useTranslation } from 'react-i18next';

import { useAuthStore } from '@/shared/auth/auth-store';
import { PageHeader } from '@/shared/components/page';
import { Badge, Card, CardContent, CardHeader, CardTitle } from '@/shared/ui';

export function ProfilePage() {
  const { t } = useTranslation('identity');
  const profile = useAuthStore((state) => state.profile);
  if (!profile) {
    return null;
  }

  const groups = Object.entries(
    profile.permissions.reduce<Record<string, string[]>>((result, key) => {
      const module = key.split('.')[0] ?? key;
      (result[module] ??= []).push(key);
      return result;
    }, {}),
  );

  return (
    <div className="space-y-6">
      <PageHeader title={t('profile.title')} description={profile.displayName ?? profile.userName} />
      <div className="grid gap-6 md:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>{profile.userName}</CardTitle>
          </CardHeader>
          <CardContent className="grid gap-3 text-sm">
            <p>
              <span className="text-muted-foreground">{t('profile.email')}: </span>
              {profile.email}
            </p>
            <p>
              <span className="text-muted-foreground">{t('profile.tenant')}: </span>
              {profile.tenant?.name}
            </p>
            <div className="flex flex-wrap items-center gap-2">
              <span className="text-muted-foreground">{t('profile.roles')}:</span>
              {profile.roles.map((role) => (
                <Badge key={role} variant="secondary">
                  {role}
                </Badge>
              ))}
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle>{t('profile.permissions')}</CardTitle>
          </CardHeader>
          <CardContent className="grid gap-3">
            {groups.map(([module, keys]) => (
              <div key={module} className="space-y-1">
                <p className="text-sm font-medium capitalize">{module}</p>
                <div className="flex flex-wrap gap-1">
                  {keys.map((key) => (
                    <Badge key={key} variant="outline" className="font-mono">
                      {key.split('.').slice(1).join('.')}
                    </Badge>
                  ))}
                </div>
              </div>
            ))}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
