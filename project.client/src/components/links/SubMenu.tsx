import { Popover, PopoverContent, PopoverTrigger } from "@heroui/popover";
import { AnimatePresence, motion } from "framer-motion";
import { useState, useRef } from "react";
import { Link, useLocation } from "react-router";
import type { Authorizations } from "../../types/Authorizations";

export function SubMenu({ data, isCollapsed }: { readonly data: Authorizations, readonly isCollapsed?: boolean }) {
  const { pathname } = useLocation();
  const [subMenuOpen, setSubMenuOpen] = useState(false);
  const [isHovered, setIsHovered] = useState(false);
  let timeoutRef = useRef<NodeJS.Timeout | undefined>(undefined);

  const toggleSubMenu = () => {
    if (!isCollapsed) setSubMenuOpen(!subMenuOpen);
  };

  const isActive = data.operations.some((op) => pathname.includes(op.path));

  // Determine Icon name or use a default one for dynamic modules
  const getIcon = (name: string) => {
    switch (name.toLowerCase()) {
      case "mantenimientos": return "bi-tools";
      case "consultas": return "bi-search";
      case "reportes": return "bi-file-earmark-bar-graph";
      case "procesos": return "bi-gear-wide-connected";
      case "seguridad": return "bi-shield-lock";
      default: return "bi-folder2-open";
    }
  };

  const handleMouseEnter = () => {
    if (timeoutRef.current) clearTimeout(timeoutRef.current);
    if (isCollapsed) setIsHovered(true);
  };

  const handleMouseLeave = () => {
    timeoutRef.current = setTimeout(() => {
      setIsHovered(false);
    }, 150);
  };

  return (
    <li className="relative flex flex-col list-none w-full">
      <Popover 
        isOpen={isCollapsed && isHovered} 
        onOpenChange={(open) => {
          if (!open) setIsHovered(false);
        }}
        placement="right-start" 
        offset={12} 
        showArrow={true}
        classNames={{
          base: [
            "before:bg-gray-200 dark:before:bg-zinc-700",
          ],
          content: [
            "border border-gray-200 bg-white p-2 shadow-xl dark:border-zinc-700 dark:bg-zinc-900 rounded-lg min-w-[200px]"
          ]
        }}
      >
        <PopoverTrigger>
          <button
            onClick={toggleSubMenu}
            onMouseEnter={handleMouseEnter}
            onMouseLeave={handleMouseLeave}
            className={`flex w-full items-center rounded-lg py-2.5 text-[0.9rem] font-bold transition-all
              ${
                 isActive || subMenuOpen 
                   ? "bg-blue-50/50 text-blue-700 dark:bg-zinc-800/80 dark:text-blue-400"
                   : "text-gray-700 hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-zinc-800/50"
              }
              ${isCollapsed ? "justify-center px-0" : "justify-between px-3"}
            `}
          >
            <div className={`flex items-center ${isCollapsed ? "justify-center mx-auto" : ""}`}>
              <div className="flex items-center justify-center w-6 h-6 shrink-0">
                <i className={`bi ${getIcon(data.module.name)} text-xl`} />
              </div>
              
              <div className={`flex items-center overflow-hidden transition-all duration-300 ${isCollapsed ? "w-0 opacity-0" : "w-auto opacity-100 ml-3"}`}>
                <span className="tracking-wide whitespace-nowrap">
                  {data.module.name}
                </span>
              </div>
            </div>
            
            {/* Chevron para versión expandida */}
            <div className={`flex items-center overflow-hidden transition-all ${isCollapsed ? "w-0 opacity-0" : "w-auto opacity-100"}`}>
              <i className={`bi bi-chevron-down text-sm transition-transform duration-300 ${subMenuOpen ? "rotate-180" : ""}`} />
            </div>
          </button>
        </PopoverTrigger>

        {/* --- MENÚ FLOTANTE VÍA POPOVER (PORTAL - NO SE CORTA) --- */}
        <PopoverContent onMouseEnter={handleMouseEnter} onMouseLeave={handleMouseLeave}>
          <div className="w-full">
            <div className="mb-2 border-b border-gray-100 pb-2 px-2 text-[0.7rem] font-black uppercase text-gray-500 dark:border-zinc-800 dark:text-gray-400">
              {data.module.name}
            </div>
            <ul className="flex flex-col gap-0.5 w-full">
              {data.operations.map((menu) => (
                <li key={menu.path} className="w-full">
                  <Link
                    viewTransition
                    className={`block w-full rounded-md px-3 py-2 text-sm font-medium transition-colors ${
                      pathname === menu.path
                        ? "bg-blue-50 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400"
                        : "text-gray-600 hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-zinc-800/80"
                    }`}
                    to={menu.path}
                    onClick={() => setIsHovered(false)}
                  >
                    {menu.name}
                  </Link>
                </li>
              ))}
            </ul>
          </div>
        </PopoverContent>
      </Popover>

      {/* --- ACORDEÓN INLINE (ESTADO EXPANDIDO) --- */}
      <AnimatePresence>
        {!isCollapsed && subMenuOpen && (
          <motion.ul
            initial={{ height: 0, opacity: 0 }}
            animate={{ height: "auto", opacity: 1 }}
            exit={{ height: 0, opacity: 0 }}
            transition={{ duration: 0.3, ease: "easeInOut" }}
            className="flex flex-col gap-1 overflow-hidden pl-9 pr-2 mt-1"
          >
            {data.operations.map((menu) => (
              <li key={menu.path}>
                <Link
                  viewTransition
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
        )}
      </AnimatePresence>
    </li>
  );
}
