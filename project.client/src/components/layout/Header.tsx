import { Button } from "@heroui/button";
import { useTheme } from "next-themes";
import { useCallback, useEffect, useState } from "react";

interface HeaderProps {
  readonly toggleSidebar: () => void;
}

export function Header({ toggleSidebar }: HeaderProps) {
  const { setTheme, resolvedTheme } = useTheme();
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
  }, []);

  const handleThemeChange = useCallback(() => {
    // Si la transición de vista de la API del navegador daba saltos visuales
    // a veces es porque NextThemes cambia la clase fuera del ciclo de reconciliación de React.
    // Ahora le indicaremos explicitamente el cambio de light/dark.
    const newTheme = resolvedTheme === "dark" ? "light" : "dark";
    if (document.startViewTransition) {
      document.startViewTransition(() => {
        setTheme(newTheme);
      });
    } else {
      setTheme(newTheme);
    }
  }, [setTheme, resolvedTheme]);

  return (
    <header className="sticky top-0 z-40 flex h-16 w-full items-center justify-between border-b border-gray-200 bg-white/80 px-4 backdrop-blur-md dark:border-zinc-800 dark:bg-[#18181b]/80 transition-colors duration-300">
      <Button
        aria-label="Toggle Sidebar"
        className="flex min-w-0 bg-transparent items-center justify-center rounded-md p-2 text-gray-600 hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-zinc-800 transition-colors"
        onClick={toggleSidebar}
      >
        <i className="bi bi-list text-2xl" />
      </Button>

      <div className="flex items-center gap-4">
        {mounted ? (
          <Button
            className="flex h-10 w-10 min-w-0 items-center justify-center rounded-full bg-gray-100 text-gray-700 hover:bg-gray-200 dark:bg-zinc-800 dark:text-gray-300 dark:hover:bg-zinc-700 transition-colors"
            title="Alternar tema"
            onClick={handleThemeChange}
          >
            {resolvedTheme === "dark" ? (
              <i className="bi bi-sun-fill text-lg" />
            ) : (
              <i className="bi bi-moon-stars-fill text-lg" />
            )}
          </Button>
        ) : null}
      </div>
    </header>
  );
}
