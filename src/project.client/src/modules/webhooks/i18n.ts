import type { ModuleResources } from '@/shared/i18n';

export const resources: ModuleResources = {
  es: {
    title: 'Webhooks',
    description:
      'Envía los eventos de la aplicación a tus otros sistemas. Cada envío va firmado con HMAC-SHA256 y se reintenta si falla.',
    new: 'Nuevo webhook',
    name: 'Nombre',
    url: 'URL (https)',
    events: 'Eventos',
    allEvents: 'Todos los eventos',
    active: 'Activo',
    secretTitle: 'Guarda el secreto de firma',
    secretHint:
      'Úsalo para verificar la cabecera X-Webhook-Signature (t=marca,v1=HMAC-SHA256 de "marca.cuerpo"). No se volverá a mostrar.',
    ping: 'Enviar prueba',
    pinged: 'Prueba encolada.',
    deliveries: 'Entregas',
    retry: 'Reintentar',
    attempts: '{{count}} intentos',
    statuses: { Pending: 'Pendiente', Delivered: 'Entregado', Failed: 'Fallido' },
    noDeliveries: 'Sin entregas todavía.',
  },
  en: {
    title: 'Webhooks',
    description:
      'Send the events of the application to your other systems. Every call is signed with HMAC-SHA256 and retried when it fails.',
    new: 'New webhook',
    name: 'Name',
    url: 'URL (https)',
    events: 'Events',
    allEvents: 'All events',
    active: 'Active',
    secretTitle: 'Save the signing secret',
    secretHint:
      'Use it to verify the X-Webhook-Signature header (t=timestamp,v1=HMAC-SHA256 of "timestamp.body"). It will not be shown again.',
    ping: 'Send test',
    pinged: 'Test queued.',
    deliveries: 'Deliveries',
    retry: 'Retry',
    attempts: '{{count}} attempts',
    statuses: { Pending: 'Pending', Delivered: 'Delivered', Failed: 'Failed' },
    noDeliveries: 'No deliveries yet.',
  },
};
