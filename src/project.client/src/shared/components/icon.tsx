import {
  BarChart3Icon,
  Building2Icon,
  CircleIcon,
  GitBranchIcon,
  SquareCheckIcon,
  ClockIcon,
  FolderOpenIcon,
  HardDriveIcon,
  HashIcon,
  HistoryIcon,
  HomeIcon,
  LayersIcon,
  MailIcon,
  NetworkIcon,
  PackageIcon,
  SettingsIcon,
  SlidersHorizontalIcon,
  ShieldCheckIcon,
  TagsIcon,
  UserIcon,
  UsersIcon,
  WebhookIcon,
  type LucideIcon,
} from 'lucide-react';

/**
 * Icons referenced by name from the backend navigation. Add an entry when a module declares a new icon name.
 */
const icons: Record<string, LucideIcon> = {
  'bar-chart-3': BarChart3Icon,
  'building-2': Building2Icon,
  'check-square': SquareCheckIcon,
  'git-branch': GitBranchIcon,
  clock: ClockIcon,
  'folder-open': FolderOpenIcon,
  'hard-drive': HardDriveIcon,
  hash: HashIcon,
  history: HistoryIcon,
  home: HomeIcon,
  layers: LayersIcon,
  mail: MailIcon,
  network: NetworkIcon,
  package: PackageIcon,
  settings: SettingsIcon,
  'sliders-horizontal': SlidersHorizontalIcon,
  'shield-check': ShieldCheckIcon,
  tags: TagsIcon,
  user: UserIcon,
  users: UsersIcon,
  webhook: WebhookIcon,
};

export function Icon({ name, className }: { name: string; className?: string }) {
  const Component = icons[name] ?? CircleIcon;
  return <Component className={className} aria-hidden />;
}
