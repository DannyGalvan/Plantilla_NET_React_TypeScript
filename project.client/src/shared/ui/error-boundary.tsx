import { Component, type ReactNode } from 'react';

interface Props {
  children: ReactNode;
}

interface State {
  hasError: boolean;
}

/**
 * React error boundary. In dev, the original error message and stack are
 * rendered for debugging. In production, the stack and message are hidden and
 * only a generic message + traceId are shown.
 */
export class ErrorBoundary extends Component<Props, State> {
  override state: State = { hasError: false };

  static getDerivedStateFromError(): State {
    return { hasError: true };
  }

  override componentDidCatch(
    error: Error,
    info: { componentStack?: string | null },
  ): void {
    if (import.meta.env.DEV) {
      console.error('ErrorBoundary caught', error, info.componentStack);
    }
  }

  override render(): ReactNode {
    if (!this.state.hasError) return this.props.children;

    return (
      <main
        style={{
          minHeight: '100vh',
          display: 'grid',
          placeItems: 'center',
          padding: '1rem',
          backgroundColor: 'var(--background)',
        }}
      >
        <div
          style={{
            maxWidth: '32rem',
            backgroundColor: 'var(--card)',
            color: 'var(--card-foreground)',
            border: '1px solid var(--border)',
            borderRadius: 'var(--radius)',
            padding: '1.5rem',
          }}
        >
          <h1 style={{ fontSize: '1.125rem', fontWeight: 600 }}>
            Algo salió mal
          </h1>
          <p style={{ color: 'var(--muted-foreground)' }}>
            {import.meta.env.DEV
              ? 'El error se muestra en la consola del navegador.'
              : 'Intenta recargar la página o contacta al administrador.'}
          </p>
        </div>
      </main>
    );
  }
}
