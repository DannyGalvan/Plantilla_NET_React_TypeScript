import { AnimatePresence, motion } from "framer-motion";
import { useCallback, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { Link, useLocation } from "react-router";
import type { Authorizations } from "../../types/Authorizations";

export function SubMenu({
  data,
  isCollapsed,
}: {
  readonly data: Authorizations;
  readonly isCollapsed?: boolean;
}) {
  const { pathname } = useLocation();
  const [subMenuOpen, setSubMenuOpen] = useState(false);
  const [isHovered, setIsHovered] = useState(false);
  const [btnRect, setBtnRect] = useState<{
    top: number;
    left: number;
    width: number;
  } | null>(null);
  const timeoutRef = useRef<NodeJS.Timeout | undefined>(undefined);
  const buttonRef = useRef<HTMLButtonElement>(null);

  const toggleSubMenu = useCallback(() => {
    if (!isCollapsed) setSubMenuOpen((prev) => !prev);
  }, [isCollapsed]);

  const isActive = data.operations.some((op) => pathname.includes(op.path));

  const handleMouseEnter = useCallback(() => {
    if (timeoutRef.current) clearTimeout(timeoutRef.current);
    if (isCollapsed && buttonRef.current) {
      const rect = buttonRef.current.getBoundingClientRect();
      setBtnRect({ top: rect.top, left: rect.left, width: rect.width });
      setIsHovered(true);
    }
  }, [isCollapsed]);

  const handleMouseLeave = useCallback(() => {
    timeoutRef.current = setTimeout(() => {
      setIsHovered(false);
    }, 100);
  }, []);

  const handleLinkClick = useCallback(() => {
    setIsHovered(false);
  }, []);

  return (
    <li
      className="relative flex flex-col list-none w-full"
      onMouseEnter={handleMouseEnter}
      onMouseLeave={handleMouseLeave}
    >
      <button
        ref={buttonRef}
        className={`relative flex w-full items-center rounded-lg py-2.5 text-[0.9rem] font-bold transition-all
            ${
              isActive || subMenuOpen
                ? "bg-blue-50/50 text-blue-700 dark:bg-zinc-800/80 dark:text-blue-400"
                : "text-gray-700 hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-zinc-800/50"
            }
            ${isCollapsed ? "justify-center px-0" : "justify-between px-3"}
          `}
        type="button"
        onClick={toggleSubMenu}
      >
        <div className="flex items-center justify-center">
          <div className="flex items-center justify-center w-6 h-6 shrink-0">
            <i className={`${data.module.image} text-xl`} />
          </div>

          <div
            className={`flex items-center transition-all duration-300 overflow-hidden ${isCollapsed ? "opacity-0 w-0" : "w-auto opacity-100 ml-3"}`}
          >
            <span className="tracking-wide whitespace-nowrap">
              {data.module.name}
            </span>
          </div>
        </div>

        {/* Chevron para versión expandida */}
        {!isCollapsed && (
          <div className="flex items-center transition-all opacity-100">
            <i
              className={`bi bi-chevron-down text-sm transition-transform duration-300 ${subMenuOpen ? "rotate-180" : ""}`}
            />
          </div>
        )}
      </button>

      {/* --- MENÚ FLOTANTE CUSTOM (SIN PARPADEO Y SIN REACT ARIA POPOVER) --- */}
      {isCollapsed && isHovered && btnRect && typeof document !== "undefined"
        ? createPortal(
            <div
              className="fixed z-9999 pl-3"
              style={{
                top: btnRect.top - 8,
                left: btnRect.left + btnRect.width,
              }}
              onMouseEnter={handleMouseEnter}
              onMouseLeave={handleMouseLeave}
            >
              <motion.div
                animate={{ opacity: 1, x: 0, scale: 1 }}
                className="relative border border-gray-200 bg-white p-2 shadow-xl dark:border-zinc-700 dark:bg-zinc-900 rounded-lg min-w-50"
                initial={{ opacity: 0, x: -5, scale: 0.98 }}
                transition={{ duration: 0.15 }}
              >
                {/* Flecha apuntando al icono */}
                <div className="absolute top-[1.1rem] -left-[5.5px] w-2.5 h-2.5 rotate-45 border-b border-l border-gray-200 bg-white dark:border-zinc-700 dark:bg-zinc-900" />

                <div className="mb-2 border-b border-gray-100 pb-2 px-2 text-[0.7rem] font-black uppercase text-gray-500 dark:border-zinc-800 dark:text-gray-400">
                  {data.module.name}
                </div>
                <ul className="flex flex-col gap-0.5 w-full">
                  {data.operations
                    .filter((x) => x.isVisible)
                    .map((menu) => (
                      <li key={menu.path} className="w-full">
                        <Link
                          className={`block w-full rounded-md px-3 py-2 text-sm font-medium transition-colors ${
                            pathname === menu.path
                              ? "bg-blue-50 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400"
                              : "text-gray-600 hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-zinc-800/80"
                          }`}
                          to={menu.path}
                          onClick={handleLinkClick}
                        >
                          {menu.name}
                        </Link>
                      </li>
                    ))}
                </ul>
              </motion.div>
            </div>,
            document.body,
          )
        : null}

      {/* --- ACORDEÓN INLINE (ESTADO EXPANDIDO) --- */}
      <AnimatePresence>
        {!isCollapsed && subMenuOpen ? (
          <motion.ul
            animate={{ height: "auto", opacity: 1 }}
            className="flex flex-col gap-1 overflow-hidden pl-9 pr-2 mt-1"
            exit={{ height: 0, opacity: 0 }}
            initial={{ height: 0, opacity: 0 }}
            transition={{ duration: 0.3, ease: "easeInOut" }}
          >
            {data.operations
              .filter((x) => x.isVisible)
              .map((menu) => (
                <li key={menu.path}>
                  <Link
                    className={`block rounded-md px-3 py-2 text-sm font-medium transition-colors ${
                      pathname === menu.path
                        ? "bg-blue-100/50 text-blue-700 dark:bg-zinc-800 dark:text-blue-400"
                        : "text-gray-500 hover:bg-gray-100 dark:text-gray-400 dark:hover:bg-zinc-800/50 hover:text-gray-900 dark:hover:text-white"
                    }`}
                    to={menu.path}
                  >
                    {menu.name}
                  </Link>
                </li>
              ))}
          </motion.ul>
        ) : null}
      </AnimatePresence>
    </li>
  );
}
