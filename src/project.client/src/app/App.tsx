import { QueryClientProvider, type QueryClient } from '@tanstack/react-query';
import { RouterProvider } from 'react-router';
import { Toaster } from 'sonner';

import { ErrorBoundary } from '@/shared/components/errors';
import { ThemeProvider } from '@/shared/theme/theme-provider';
import { useTheme } from '@/shared/theme/theme-context';
import { TooltipProvider } from '@/shared/ui';

import type { createAppRouter } from './router';

function ThemedToaster() {
  const { resolvedMode } = useTheme();
  return <Toaster theme={resolvedMode} richColors closeButton position="top-right" />;
}

export function App({ router, queryClient }: { router: ReturnType<typeof createAppRouter>; queryClient: QueryClient }) {
  return (
    <ErrorBoundary>
      <QueryClientProvider client={queryClient}>
        <ThemeProvider>
          <TooltipProvider delayDuration={300}>
            <RouterProvider router={router} />
            <ThemedToaster />
          </TooltipProvider>
        </ThemeProvider>
      </QueryClientProvider>
    </ErrorBoundary>
  );
}
