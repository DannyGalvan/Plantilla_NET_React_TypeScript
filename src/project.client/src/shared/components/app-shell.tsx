import { useQuery } from '@tanstack/react-query';
import {
  ChevronsLeftIcon,
  ChevronsRightIcon,
  HomeIcon,
  KeyRoundIcon,
  LogOutIcon,
  MenuIcon,
  PanelLeftIcon,
  UserIcon,
} from 'lucide-react';
import { useEffect, useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, NavLink, Outlet, useMatches, useNavigate } from 'react-router';

import { useAuthStore } from '@/shared/auth/auth-store';
import { usePermission } from '@/shared/auth/permissions';
import { authApi } from '@/shared/auth/session';
import type { NavigationItem } from '@/shared/auth/types';
import { cn } from '@/shared/lib/cn';
import type { RouteHandle } from '@/shared/modules/manifest';
import { getActiveModules } from '@/shared/modules/active-modules';
import { ContextSwitcher } from '@/shared/platform/context-switcher';
import { usePlatformStore } from '@/shared/platform/platform-config';
import { ThemeCustomizer } from '@/shared/theme/theme-customizer';
import {
  Avatar,
  AvatarFallback,
  Button,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  Sheet,
  SheetContent,
  SheetTitle,
  Tooltip,
} from '@/shared/ui';

import { ForbiddenPage } from './errors';
import { Icon } from './icon';
import { useSidebarState } from './sidebar-state';

function useNavigation() {
  const tenantId = useAuthStore((state) => state.profile?.tenant?.id);
  const version = useAuthStore((state) => state.profile?.permissionsVersion);
  return useQuery({
    queryKey: [tenantId, 'me', 'navigation', version],
    queryFn: () => authApi.navigation(),
    staleTime: 60_000,
  });
}

function NavigationList({
  items,
  collapsed = false,
  onNavigate,
}: {
  items: NavigationItem[];
  collapsed?: boolean;
  onNavigate?: () => void;
}) {
  const { t } = useTranslation();
  const linkClass = ({ isActive }: { isActive: boolean }) =>
    cn(
      'flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors hover:bg-sidebar-accent hover:text-sidebar-accent-foreground',
      collapsed && 'justify-center px-0',
      isActive && 'bg-sidebar-accent text-sidebar-accent-foreground',
    );
  const entries = [
    {
      key: 'home',
      to: '/',
      end: true,
      icon: <HomeIcon className="size-4 shrink-0" aria-hidden />,
      label: t('nav.dashboard'),
    },
    ...items.map((item) => ({
      key: item.key,
      to: item.path,
      end: false,
      icon: <Icon name={item.icon} className="size-4 shrink-0" />,
      label: t(item.labelKey),
    })),
  ];

  return (
    <nav className="flex flex-col gap-1" aria-label={t('shell.navigation')}>
      {entries.map((entry) => {
        const link = (
          <NavLink
            key={entry.key}
            to={entry.to}
            end={entry.end}
            className={linkClass}
            aria-label={collapsed ? entry.label : undefined}
            onClick={onNavigate}
          >
            {entry.icon}
            {!collapsed && <span className="truncate">{entry.label}</span>}
          </NavLink>
        );
        return collapsed ? (
          <SidebarTooltip key={entry.key} label={entry.label}>
            {link}
          </SidebarTooltip>
        ) : (
          link
        );
      })}
    </nav>
  );
}

function SidebarTooltip({ label, children }: { label: string; children: ReactNode }) {
  return (
    <Tooltip content={label} side="right">
      {children}
    </Tooltip>
  );
}

function Brand({ collapsed = false }: { collapsed?: boolean }) {
  const branding = usePlatformStore((state) => state.config.branding);
  return (
    <Link
      to="/"
      className={cn('flex items-center gap-2 px-3 py-1 font-semibold', collapsed && 'justify-center px-0')}
      aria-label={collapsed ? branding.displayName : undefined}
    >
      {branding.logoUrl ? (
        <img src={branding.logoUrl} alt="" className="size-7 shrink-0 rounded-md" />
      ) : (
        <span className="flex size-7 shrink-0 items-center justify-center rounded-md bg-sidebar-primary text-sm text-sidebar-primary-foreground">
          {branding.displayName.slice(0, 1)}
        </span>
      )}
      {!collapsed && <span className="truncate">{branding.displayName}</span>}
    </Link>
  );
}

function UserMenu() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const profile = useAuthStore((state) => state.profile);
  const name = profile?.displayName ?? profile?.userName ?? '';

  const signOut = async () => {
    await authApi.logout();
    await navigate('/login');
  };

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon" className="rounded-full" aria-label={name}>
          <Avatar>
            <AvatarFallback>{name.slice(0, 2).toUpperCase()}</AvatarFallback>
          </Avatar>
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-56">
        <DropdownMenuLabel>
          <p className="truncate">{name}</p>
          <p className="truncate text-xs font-normal text-muted-foreground">{profile?.email}</p>
        </DropdownMenuLabel>
        <DropdownMenuSeparator />
        <DropdownMenuItem onSelect={() => void navigate('/profile')}>
          <UserIcon />
          {t('nav.profile')}
        </DropdownMenuItem>
        <DropdownMenuItem onSelect={() => void navigate('/change-password')}>
          <KeyRoundIcon />
          {t('identity:changePassword.title')}
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem variant="destructive" onSelect={() => void signOut()}>
          <LogOutIcon />
          {t('actions.signOut')}
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

/** Authenticated layout: sidebar from the server navigation (already filtered by permission), top bar and content. */
export function AppShell() {
  const topbarItems = getActiveModules().flatMap((module) => module.topbarItems ?? []);
  const { t } = useTranslation();
  const navigation = useNavigation();
  const [open, setOpen] = useState(false);
  const sidebar = useSidebarState();
  const collapsed = sidebar.state === 'collapsed';
  const { toggleCollapsed } = sidebar;
  const matches = useMatches();
  const handles = [...matches].reverse().map((match) => match.handle as RouteHandle | undefined);
  const title = handles.map((handle) => handle?.titleKey).find(Boolean);
  const permission = handles.map((handle) => handle?.permission).find(Boolean);
  const allowed = usePermission(permission);

  useEffect(() => {
    // Ctrl/Cmd+B expands or collapses the sidebar, as in most editors.
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key.toLowerCase() === 'b' && (event.ctrlKey || event.metaKey) && !event.altKey && !event.shiftKey) {
        event.preventDefault();
        toggleCollapsed();
      }
    };
    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [toggleCollapsed]);

  return (
    <div className="flex min-h-screen bg-background">
      {sidebar.state !== 'hidden' && (
        // Sticky and viewport-tall: the menu scrolls on its own, independent of the page.
        <aside
          className={cn(
            'sticky top-0 hidden h-dvh shrink-0 flex-col border-r bg-sidebar text-sidebar-foreground transition-[width] duration-200 lg:flex',
            collapsed ? 'w-16' : 'w-64',
          )}
        >
          <div className="flex h-14 shrink-0 items-center px-3">
            <Brand collapsed={collapsed} />
          </div>
          <div className="min-h-0 flex-1 overflow-y-auto overscroll-contain px-3 py-2">
            <NavigationList items={navigation.data ?? []} collapsed={collapsed} />
          </div>
          <div className="shrink-0 border-t p-3">
            <Button
              variant="ghost"
              size="sm"
              className={cn('w-full justify-start', collapsed && 'justify-center px-0')}
              aria-label={collapsed ? t('shell.expand') : t('shell.collapse')}
              title={`${collapsed ? t('shell.expand') : t('shell.collapse')} (Ctrl+B)`}
              onClick={toggleCollapsed}
            >
              {collapsed ? <ChevronsRightIcon aria-hidden /> : <ChevronsLeftIcon aria-hidden />}
              {!collapsed && t('shell.collapse')}
            </Button>
          </div>
        </aside>
      )}

      <Sheet open={open} onOpenChange={setOpen}>
        <SheetContent side="left" className="flex w-72 flex-col gap-4 bg-sidebar p-3 text-sidebar-foreground">
          <SheetTitle className="sr-only">{t('shell.navigation')}</SheetTitle>
          <Brand />
          <div className="min-h-0 flex-1 overflow-y-auto overscroll-contain">
            <NavigationList items={navigation.data ?? []} onNavigate={() => setOpen(false)} />
          </div>
        </SheetContent>
      </Sheet>

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="sticky top-0 z-30 flex h-14 items-center gap-2 border-b bg-background/95 px-4 backdrop-blur">
          <Button
            variant="ghost"
            size="icon"
            className="lg:hidden"
            aria-label={t('shell.openMenu')}
            onClick={() => setOpen(true)}
          >
            <MenuIcon />
          </Button>
          <Button
            variant="ghost"
            size="icon"
            className="hidden lg:inline-flex"
            aria-label={sidebar.state === 'hidden' ? t('shell.show') : t('shell.hide')}
            aria-pressed={sidebar.state !== 'hidden'}
            title={sidebar.state === 'hidden' ? t('shell.show') : t('shell.hide')}
            onClick={sidebar.toggleHidden}
          >
            <PanelLeftIcon />
          </Button>
          <p className="truncate text-sm font-medium text-muted-foreground">{title ? t(title) : ''}</p>
          <div className="ml-auto flex items-center gap-1">
            <div className="hidden md:block">
              <ContextSwitcher />
            </div>
            {topbarItems.map((Item, index) => (
              <Item key={index} />
            ))}
            <ThemeCustomizer />
            <UserMenu />
          </div>
        </header>
        <main className="mx-auto w-full max-w-7xl flex-1 space-y-6 p-4 md:p-6">
          <div className="md:hidden">
            <ContextSwitcher />
          </div>
          {allowed ? <Outlet /> : <ForbiddenPage />}
        </main>
      </div>
    </div>
  );
}
