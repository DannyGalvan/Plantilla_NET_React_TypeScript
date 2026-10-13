import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { BellIcon } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router';

import { useQueryScope } from '@/shared/api/query-keys';
import { formatDate } from '@/shared/lib/format';
import {
  Button,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/shared/ui';

import { notificationsApi } from './api';

/** Inbox of the signed-in user in the top bar. The unread count is refreshed every minute. */
export function NotificationBell() {
  const { t } = useTranslation('notifications');
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const scope = useQueryScope('notifications');
  const count = useQuery({
    queryKey: [...scope, 'count'],
    queryFn: notificationsApi.unreadCount,
    refetchInterval: 60_000,
  });
  const inbox = useQuery({ queryKey: [...scope, 'inbox'], queryFn: () => notificationsApi.inbox() });
  const refresh = () => queryClient.invalidateQueries({ queryKey: scope });
  const markRead = useMutation({ mutationFn: notificationsApi.markRead, onSuccess: refresh });
  const markAll = useMutation({ mutationFn: notificationsApi.markAllRead, onSuccess: refresh });
  const unread = count.data ?? 0;

  return (
    <DropdownMenu onOpenChange={(open) => open && void inbox.refetch()}>
      <DropdownMenuTrigger asChild>
        <Button
          variant="ghost"
          size="icon"
          className="relative"
          aria-label={unread ? `${t('bell.title')}: ${t('bell.unread', { count: unread })}` : t('bell.title')}
        >
          <BellIcon aria-hidden />
          {unread > 0 && (
            <span className="absolute top-1 right-1 flex h-4 min-w-4 items-center justify-center rounded-full bg-destructive px-1 text-[10px] font-semibold text-destructive-foreground">
              {unread > 9 ? '9+' : unread}
            </span>
          )}
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-80">
        <DropdownMenuLabel className="flex items-center justify-between">
          {t('bell.title')}
          {unread > 0 && (
            <Button variant="link" size="sm" className="h-auto p-0 text-xs" onClick={() => markAll.mutate()}>
              {t('bell.markAll')}
            </Button>
          )}
        </DropdownMenuLabel>
        <DropdownMenuSeparator />
        {inbox.data?.items.length ? (
          inbox.data.items.map((notification) => (
            <DropdownMenuItem
              key={notification.id}
              className="flex flex-col items-start gap-0.5"
              onSelect={() => {
                if (!notification.readAt) {
                  markRead.mutate(notification.id);
                }

                if (notification.link) {
                  void navigate(notification.link);
                }
              }}
            >
              <span className={notification.readAt ? 'text-sm' : 'text-sm font-semibold'}>{notification.title}</span>
              <span className="line-clamp-2 text-xs text-muted-foreground">{notification.body}</span>
              <span className="text-[10px] text-muted-foreground">{formatDate(notification.createdAt)}</span>
            </DropdownMenuItem>
          ))
        ) : (
          <p className="px-2 py-6 text-center text-sm text-muted-foreground">{t('bell.empty')}</p>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
