import { useAuthStore } from '../../../shared/auth/auth-store';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '../../../shared/ui/card';

const tiles = [
  { label: 'Productos', value: '—', hint: 'Total en catálogo' },
  { label: 'Categorías', value: '—', hint: 'Activas' },
  { label: 'Usuarios', value: '—', hint: 'En este tenant' },
  { label: 'Tickets', value: '—', hint: 'Abiertos' },
];

export function DashboardPage() {
  const user = useAuthStore((s) => s.user);

  return (
    <main style={{ padding: '2rem', maxWidth: '80rem', margin: '0 auto' }}>
      <header style={{ marginBottom: '1.5rem' }}>
        <h1 style={{ fontSize: '1.5rem', fontWeight: 600 }}>
          Hola, {user?.userName ?? 'invitado'}
        </h1>
        <p style={{ color: 'var(--muted-foreground)' }}>
          Resumen rápido del tenant {user?.tenantId ?? '—'}.
        </p>
      </header>
      <section
        style={{
          display: 'grid',
          gap: '1rem',
          gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))',
        }}
      >
        {tiles.map((tile) => (
          <Card key={tile.label}>
            <CardHeader>
              <CardTitle>{tile.label}</CardTitle>
              <CardDescription>{tile.hint}</CardDescription>
            </CardHeader>
            <CardContent>
              <p style={{ fontSize: '2rem', fontWeight: 700 }}>{tile.value}</p>
            </CardContent>
          </Card>
        ))}
      </section>
    </main>
  );
}
