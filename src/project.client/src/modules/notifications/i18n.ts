import type { ModuleResources } from '@/shared/i18n';

export const resources: ModuleResources = {
  es: {
    bell: {
      title: 'Notificaciones',
      empty: 'No tienes notificaciones.',
      markAll: 'Marcar todas como leídas',
      unread: '{{count}} sin leer',
    },
    templates: {
      title: 'Plantillas de notificación',
      description:
        'Textos de las notificaciones y correos que envían los módulos. Usa {{placeholder}} para insertar datos.',
      subject: 'Asunto',
      body: 'Mensaje',
      customized: 'Personalizada',
      placeholders: 'Datos disponibles',
      reset: 'Restaurar original',
      sendTest: 'Enviarme una prueba',
      testSent: 'Notificación de prueba enviada.',
    },
  },
  en: {
    bell: {
      title: 'Notifications',
      empty: 'You have no notifications.',
      markAll: 'Mark all as read',
      unread: '{{count}} unread',
    },
    templates: {
      title: 'Notification templates',
      description: 'Texts of the notifications and emails sent by the modules. Use {{placeholder}} to insert data.',
      subject: 'Subject',
      body: 'Message',
      customized: 'Customized',
      placeholders: 'Available data',
      reset: 'Restore default',
      sendTest: 'Send me a test',
      testSent: 'Test notification sent.',
    },
  },
};
