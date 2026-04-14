import { Link } from "react-router";
import { nameRoutes } from "../../configs/constants";

export function Footer() {
  const currentYear = new Date().getFullYear();

  return (
    <footer className="flex w-full flex-col items-center justify-between gap-4 border-t border-gray-200 bg-white/80 backdrop-blur-md px-6 py-4 text-sm text-gray-500 transition-colors dark:border-zinc-800 dark:bg-[#18181b]/90 dark:text-gray-400 sm:flex-row shadow-[0_-4px_6px_-2px_rgba(0,0,0,0.05)] relative z-10">
      <div className="flex flex-col sm:flex-row items-center gap-1">
        <span className="font-semibold text-gray-900 dark:text-white">
          <Link
            viewTransition
            className="hover:text-blue-600 dark:hover:text-blue-400 transition-colors"
            to={nameRoutes.root}
          >
            HIS &copy; {currentYear}
          </Link>
        </span>
        <span className="hidden sm:inline-block">.</span>
        <span>Todos los derechos reservados.</span>
      </div>

      <div className="flex items-center gap-2 font-medium text-xs">
        <span className="rounded-full bg-gray-100 px-3 py-1 text-gray-600 dark:bg-zinc-800 dark:text-gray-300 border border-gray-200 dark:border-zinc-700 shadow-inner">
          Versión{" "}
          <span className="font-bold text-gray-800 dark:text-gray-100">
            1.0.0
          </span>
        </span>
      </div>
    </footer>
  );
}
