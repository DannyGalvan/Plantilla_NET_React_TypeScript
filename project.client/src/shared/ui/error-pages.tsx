import { Link } from 'react-router';

import { Button } from './button';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from './card';

interface ErrorPageProps {
  code: 404 | 403 | 500 | 'network';
  title: string;
  description: string;
}

export function ErrorPage({ code, title, description }: ErrorPageProps) {
  const traceId =
    typeof crypto !== 'undefined' && 'randomUUID' in crypto
      ? crypto.randomUUID()
      : undefined;

  return (
    <main
      style={{
        minHeight: '100vh',
        display: 'grid',
        placeItems: 'center',
        backgroundColor: 'var(--background)',
        padding: '1rem',
      }}
    >
      <Card style={{ width: '32rem' }}>
        <CardHeader>
          <CardTitle>
            {code} — {title}
          </CardTitle>
          <CardDescription>{description}</CardDescription>
        </CardHeader>
        <CardContent>
          {import.meta.env.DEV && traceId && (
            <p
              style={{ color: 'var(--muted-foreground)', fontSize: '0.75rem' }}
            >
              Trace ID (dev): {traceId}
            </p>
          )}
          <div style={{ marginTop: '1rem', display: 'flex', gap: '0.5rem' }}>
            <Link to="/">
              <Button>Volver al inicio</Button>
            </Link>
          </div>
        </CardContent>
      </Card>
    </main>
  );
}

export function NotFoundPage() {
  return (
    <ErrorPage
      code={404}
      description="La ruta solicitada no existe o fue movida."
      title="Recurso no encontrado"
    />
  );
}

export function ForbiddenPage() {
  return (
    <ErrorPage
      code={403}
      description="No tienes permiso para acceder a este recurso. Si crees que es un error, contacta al administrador."
      title="Acceso denegado"
    />
  );
}

export function ServerErrorPage() {
  return (
    <ErrorPage
      code={500}
      description="El servidor encontró un error inesperado. Intenta de nuevo en unos minutos."
      title="Error interno"
    />
  );
}
