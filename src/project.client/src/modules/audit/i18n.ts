import type { ModuleResources } from '@/shared/i18n';

export const resources: ModuleResources = {
  es: {
    title: 'Auditoría',
    description: 'Historial de cambios de todos los módulos: quién cambió qué y cuándo.',
    when: 'Fecha',
    user: 'Usuario',
    operation: 'Operación',
    record: 'Registro',
    changes: 'Cambios',
    entityType: 'Tipo (catalog.product)',
    entityId: 'Id del registro',
  },
  en: {
    title: 'Audit',
    description: 'Change history of every module: who changed what and when.',
    when: 'Date',
    user: 'User',
    operation: 'Operation',
    record: 'Record',
    changes: 'Changes',
    entityType: 'Type (catalog.product)',
    entityId: 'Record id',
  },
};
