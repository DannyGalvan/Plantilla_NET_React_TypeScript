import { useAuthStore } from '../../../shared/auth/auth-store';
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from '../../../shared/ui/card';

export function ProfilePage() {
  const user = useAuthStore((s) => s.user);

  if (!user) return null;

  return (
    <main style={{ padding: '2rem', maxWidth: '32rem', margin: '0 auto' }}>
      <Card>
        <CardHeader>
          <CardTitle>Mi perfil</CardTitle>
        </CardHeader>
        <CardContent>
          <dl
            style={{
              display: 'grid',
              gridTemplateColumns: 'auto 1fr',
              gap: '0.5rem 1rem',
            }}
          >
            <dt style={{ color: 'var(--muted-foreground)' }}>Usuario</dt>
            <dd>{user.userName}</dd>
            <dt style={{ color: 'var(--muted-foreground)' }}>Email</dt>
            <dd>{user.email}</dd>
            <dt style={{ color: 'var(--muted-foreground)' }}>Tenant</dt>
            <dd>{user.tenantId ?? '—'}</dd>
            <dt style={{ color: 'var(--muted-foreground)' }}>
              Unidad organizativa
            </dt>
            <dd>{user.organizationUnitId ?? '—'}</dd>
            <dt style={{ color: 'var(--muted-foreground)' }}>Roles</dt>
            <dd>{user.roles.join(', ')}</dd>
          </dl>
        </CardContent>
      </Card>
    </main>
  );
}
