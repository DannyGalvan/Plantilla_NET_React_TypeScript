import { Component, type ErrorInfo, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { isRouteErrorResponse, Link, useRouteError } from 'react-router';

import { ApiError } from '@/shared/api/problem-details';
import { Button } from '@/shared/ui';

function ErrorLayout({
  code,
  title,
  message,
  detail,
}: {
  code: string;
  title: string;
  message: string;
  detail?: string;
}) {
  const { t } = useTranslation();
  return (
    <main className="flex min-h-[60vh] flex-col items-center justify-center gap-4 p-6 text-center">
      <p className="text-6xl font-bold text-primary">{code}</p>
      <h1 className="text-2xl font-semibold">{title}</h1>
      <p className="max-w-md text-muted-foreground">{message}</p>
      {detail && <p className="text-xs text-muted-foreground">{detail}</p>}
      <Button asChild>
        <Link to="/">{t('actions.goHome')}</Link>
      </Button>
    </main>
  );
}

export function NotFoundPage() {
  const { t } = useTranslation();
  return <ErrorLayout code="404" title={t('errors.notFoundTitle')} message={t('errors.notFound')} />;
}

export function ForbiddenPage() {
  const { t } = useTranslation();
  return <ErrorLayout code="403" title={t('errors.forbiddenTitle')} message={t('errors.forbidden')} />;
}

/** Route `errorElement`: never shows stack traces; shows the trace id for support. */
export function RouteErrorPage() {
  const { t } = useTranslation();
  const error = useRouteError();

  if (isRouteErrorResponse(error) && error.status === 404) {
    return <NotFoundPage />;
  }

  const apiError = ApiError.from(error);
  if (apiError.status === 403) {
    return <ForbiddenPage />;
  }

  return (
    <ErrorLayout
      code={apiError.status ? String(apiError.status) : '500'}
      title={t('errors.title')}
      message={t('errors.generic')}
      detail={apiError.traceId ? t('errors.traceId', { traceId: apiError.traceId }) : undefined}
    />
  );
}

/** Last-resort boundary for render errors outside the router. Production shows no technical details. */
export class ErrorBoundary extends Component<{ children: ReactNode }, { failed: boolean }> {
  state = { failed: false };

  static getDerivedStateFromError() {
    return { failed: true };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    if (import.meta.env.DEV) {
      console.error(error, info.componentStack);
    }
  }

  render() {
    if (!this.state.failed) {
      return this.props.children;
    }

    return (
      <main className="flex min-h-screen flex-col items-center justify-center gap-4 p-6 text-center">
        <h1 className="text-2xl font-semibold">Something went wrong / Algo salió mal</h1>
        <Button onClick={() => window.location.reload()}>Reload / Recargar</Button>
      </main>
    );
  }
}
