import type { ReactNode } from "react";
import { useCallback, useState } from "react";
import { Footer } from "../components/layout/Footer";
import { Header } from "../components/layout/Header";
import { Sidebar } from "../components/layout/Sidebar";

interface LayoutProps {
  readonly children: ReactNode;
}

export function Layout({ children }: LayoutProps) {
  const [sidebarOpen, setSidebarOpen] = useState(() => {
    if (typeof window !== "undefined") {
      return window.innerWidth >= 768; // Empieza extendido en escritorio, colapsado/oculto en móvil
    }
    return false;
  });

  const closeSidebar = useCallback(() => {
    setSidebarOpen(false);
  }, []);

  const toggleSidebar = useCallback(() => {
    setSidebarOpen((prev) => !prev);
  }, []);

  return (
    <div className="flex h-screen w-full bg-slate-50 dark:bg-background transition-colors duration-300">
      <Sidebar closeSidebar={closeSidebar} isOpen={sidebarOpen} />

      <div className="flex flex-1 flex-col overflow-hidden max-h-screen relative">
        <Header toggleSidebar={toggleSidebar} />

        <div
          className="flex-1 overflow-auto px-4 py-0 pt-5 pb-5 scroll-smooth scrollbar-thin scrollbar-track-transparent scrollbar-thumb-gray-300 dark:scrollbar-thumb-zinc-700"
          id="scroll"
          style={{ scrollBehavior: "smooth" }}
        >
          <main className="h-full w-full">{children}</main>
        </div>

        {/* Footer movido fuera del contenedor scrollable para que sea Fixed (siempre visible abajo) */}
        <div className="shrink-0 w-full z-40 bg-white dark:bg-[#18181b]">
          <Footer />
        </div>
      </div>
    </div>
  );
}
