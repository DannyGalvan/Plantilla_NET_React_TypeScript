import { QueryClient } from '@tanstack/react-query';

import { ApiError } from '@/shared/api/problem-details';

export function createQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 30_000,
        refetchOnWindowFocus: false,
        // Client errors (403, 404, validation) are not retried; transient failures are retried twice.
        retry: (failureCount, error) => {
          const status = ApiError.from(error).status;
          return (status === 0 || status >= 500) && failureCount < 2;
        },
      },
    },
  });
}
