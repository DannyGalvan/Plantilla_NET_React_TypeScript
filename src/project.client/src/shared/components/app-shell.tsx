import { useQuery } from '@tanstack/react-query';
import { HomeIcon, KeyRoundIcon, LogOutIcon, MenuIcon, UserIcon } from 'lucide-react';
import { useState } from 'react';
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
} from '@/shared/ui';

import { ForbiddenPage } from './errors';
import { Icon } from './icon';

function useNavigation() {
  const tenantId = useAuthStore((state) => state.profile?.tenant?.id);
  const version = useAuthStore((state) => state.profile?.permissionsVersion);
  return useQuery({
    queryKey: [tenantId, 'me', 'navigation', version],
    queryFn: () => authApi.navigation(),
    staleTime: 60_000,
  });
}

function NavigationList({ items, onNavigate }: { items: NavigationItem[]; onNavigate?: () => void }) {
  const { t } = useTranslation();
  const linkClass = ({ isActive }: { isActive: boolean }) =>
    cn(
      'flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors hover:bg-sidebar-accent hover:text-sidebar-accent-foreground',
      isActive && 'bg-sidebar-accent text-sidebar-accent-foreground',
    );

  return (
    <nav className="flex flex-col gap-1" aria-label="Main">
      <NavLink to="/" end className={linkClass} onClick={onNavigate}>
        <HomeIcon className="size-4" aria-hidden />
        {t('nav.dashboard')}
      </NavLink>
      {items.map((item) => (
        <NavLink key={item.key} to={item.path} className={linkClass} onClick={onNavigate}>
          <Icon name={item.icon} className="size-4" />
          {t(item.labelKey)}
        </NavLink>
      ))}
    </nav>
  );
}

function Brand() {
  const branding = usePlatformStore((state) => state.config.branding);
  return (
    <Link to="/" className="flex items-center gap-2 px-3 py-1 font-semibold">
      {branding.logoUrl ? (
        <img src={branding.logoUrl} alt="" className="size-7 rounded-md" />
      ) : (
        <span className="flex size-7 items-center justify-center rounded-md bg-sidebar-primary text-sm text-sidebar-primary-foreground">
          {branding.displayName.slice(0, 1)}
        </span>
      )}
      <span className="truncate">{branding.displayName}</span>
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
  const matches = useMatches();
  const handles = [...matches].reverse().map((match) => match.handle as RouteHandle | undefined);
  const title = handles.map((handle) => handle?.titleKey).find(Boolean);
  const permission = handles.map((handle) => handle?.permission).find(Boolean);
  const allowed = usePermission(permission);

  return (
    <div className="flex min-h-screen bg-background">
      <aside className="hidden w-64 shrink-0 flex-col gap-4 border-r bg-sidebar p-3 text-sidebar-foreground lg:flex">
        <Brand />
        <NavigationList items={navigation.data ?? []} />
      </aside>

      <Sheet open={open} onOpenChange={setOpen}>
        <SheetContent side="left" className="w-72 bg-sidebar p-3 text-sidebar-foreground">
          <SheetTitle className="sr-only">Menu</SheetTitle>
          <Brand />
          <NavigationList items={navigation.data ?? []} onNavigate={() => setOpen(false)} />
        </SheetContent>
      </Sheet>

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="sticky top-0 z-30 flex h-14 items-center gap-2 border-b bg-background/95 px-4 backdrop-blur">
          <Button variant="ghost" size="icon" className="lg:hidden" aria-label="Menu" onClick={() => setOpen(true)}>
            <MenuIcon />
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
