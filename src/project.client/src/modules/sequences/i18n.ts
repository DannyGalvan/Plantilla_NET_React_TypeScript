import type { ModuleResources } from '@/shared/i18n';

export const resources: ModuleResources = {
  es: {
    title: 'Numeración de documentos',
    description:
      'Formato de los consecutivos (facturas, pedidos, solicitudes). Usa {yyyy}, {yy}, {MM}, {dd} y el contador con ceros, por ejemplo {0000}.',
    format: 'Formato',
    example: 'Ejemplo',
    reset: 'Reinicio',
    perUnit: 'Un contador por unidad',
    resets: { Never: 'Nunca', Yearly: 'Cada año', Monthly: 'Cada mes', Daily: 'Cada día' },
    restore: 'Restaurar original',
  },
  en: {
    title: 'Document numbering',
    description:
      'Format of the sequences (invoices, orders, requests). Use {yyyy}, {yy}, {MM}, {dd} and the counter with zeros, for example {0000}.',
    format: 'Format',
    example: 'Example',
    reset: 'Reset',
    perUnit: 'One counter per unit',
    resets: { Never: 'Never', Yearly: 'Every year', Monthly: 'Every month', Daily: 'Every day' },
    restore: 'Restore default',
  },
};
