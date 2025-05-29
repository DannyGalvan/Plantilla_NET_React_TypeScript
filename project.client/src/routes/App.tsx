import { HeroUIProvider } from "@heroui/system";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ReactQueryDevtools } from "@tanstack/react-query-devtools";
import { lazy, useEffect } from "react";
import { useAuth } from "../hooks/useAuth";
import ErrorBoundary from "../pages/error/ErrorBoundary";
import LoadingPage from "../pages/public/LoadingPage";

const LazyAppRoutes = lazy(() =>
  import("./AppRoutes").catch((error) => {
    console.error("Error loading AppRoutes", error);
    throw error; // Para que ErrorBoundary lo capture
  }),
);

const client = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      retry: 0,
      networkMode: "online",
      staleTime: 1000 * 60 * 5,
    },
    mutations: {
      retry: 1,
      onError: (error) => {
        console.error({ error });
      },
    },
  },
});

function App() {
  const { loading, syncAuth } = useAuth();

  useEffect(() => {
    syncAuth();
  }, [syncAuth]);

  return (
    <ErrorBoundary>
      <HeroUIProvider locale="es-ES">
        <QueryClientProvider client={client}>
          {loading ? <LoadingPage /> : <LazyAppRoutes />}
          <ReactQueryDevtools initialIsOpen={false} />
        </QueryClientProvider>
      </HeroUIProvider>
    </ErrorBoundary>
  );
}

export default App;
