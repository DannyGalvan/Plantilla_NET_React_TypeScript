import { z } from 'zod';

import { pagedSchema } from '@/shared/api/crud-api';
import { http } from '@/shared/api/http-client';

export const notificationSchema = z.object({
  id: z.string(),
  template: z.string(),
  title: z.string(),
  body: z.string(),
  link: z.string().nullable(),
  createdAt: z.string(),
  readAt: z.string().nullable(),
});

export const templateSchema = z.object({
  key: z.string(),
  module: z.string(),
  language: z.string(),
  subject: z.string(),
  body: z.string(),
  customized: z.boolean(),
  placeholders: z.array(z.string()),
});

export type AppNotification = z.infer<typeof notificationSchema>;
export type NotificationTemplate = z.infer<typeof templateSchema>;

const page = pagedSchema(notificationSchema);

export const notificationsApi = {
  inbox: async (unreadOnly = false) =>
    page.parse((await http.get('/me/notifications', { params: { unreadOnly, pageSize: 20 } })).data),
  unreadCount: async () =>
    z.object({ count: z.number() }).parse((await http.get('/me/notifications/unread-count')).data).count,
  markRead: async (id: string) => {
    await http.post(`/me/notifications/${id}/read`);
  },
  markAllRead: async () => {
    await http.post('/me/notifications/read-all');
  },
  templates: async () => z.array(templateSchema).parse((await http.get('/notifications/templates')).data),
  updateTemplate: async (key: string, language: string, subject: string, body: string) => {
    await http.put(`/notifications/templates/${key}/${language}`, { subject, body });
  },
  sendTest: async () => {
    await http.post('/notifications/test');
  },
};
