import {
  BarChart3Icon,
  Building2Icon,
  CircleIcon,
  FolderOpenIcon,
  HardDriveIcon,
  HomeIcon,
  LayersIcon,
  MailIcon,
  NetworkIcon,
  PackageIcon,
  SettingsIcon,
  ShieldCheckIcon,
  TagsIcon,
  UserIcon,
  UsersIcon,
  type LucideIcon,
} from 'lucide-react';

/**
 * Icons referenced by name from the backend navigation. Add an entry when a module declares a new icon name.
 */
const icons: Record<string, LucideIcon> = {
  'bar-chart-3': BarChart3Icon,
  'building-2': Building2Icon,
  'folder-open': FolderOpenIcon,
  'hard-drive': HardDriveIcon,
  home: HomeIcon,
  layers: LayersIcon,
  mail: MailIcon,
  network: NetworkIcon,
  package: PackageIcon,
  settings: SettingsIcon,
  'shield-check': ShieldCheckIcon,
  tags: TagsIcon,
  user: UserIcon,
  users: UsersIcon,
};

export function Icon({ name, className }: { name: string; className?: string }) {
  const Component = icons[name] ?? CircleIcon;
  return <Component className={className} aria-hidden />;
}
