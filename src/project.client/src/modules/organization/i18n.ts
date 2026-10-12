import type { ModuleResources } from '@/shared/i18n';

export const organizationResources: ModuleResources = {
  es: {
    units: {
      title: 'Organización',
      description:
        'Empresas, países, marcas y sucursales. Los usuarios ven los datos de sus unidades y de las que dependen de ellas.',
      new: 'Nueva unidad',
      newChild: 'Agregar unidad hija',
      edit: 'Editar unidad',
      type: 'Tipo',
      parent: 'Unidad padre',
      root: 'Sin padre (raíz)',
      country: 'País (ISO)',
      move: 'Mover',
      moveTo: 'Mover a',
      users: 'Usuarios asignados',
      assign: 'Asignar',
      userId: 'Usuario',
      selectUser: 'Selecciona un usuario',
      noUsers: 'Sin usuarios asignados.',
    },
    types: {
      title: 'Tipos de unidad',
      description: 'Define los niveles de tu organización sin escribir código.',
      new: 'Nuevo tipo',
      edit: 'Editar tipo',
      canBeRoot: 'Puede ser raíz',
      allowedParents: 'Padres permitidos (códigos separados por coma)',
      order: 'Orden',
    },
  },
  en: {
    units: {
      title: 'Organization',
      description:
        'Companies, countries, brands and branches. Users see the data of their units and the units below them.',
      new: 'New unit',
      newChild: 'Add child unit',
      edit: 'Edit unit',
      type: 'Type',
      parent: 'Parent unit',
      root: 'No parent (root)',
      country: 'Country (ISO)',
      move: 'Move',
      moveTo: 'Move to',
      users: 'Assigned users',
      assign: 'Assign',
      userId: 'User',
      selectUser: 'Select a user',
      noUsers: 'No users assigned.',
    },
    types: {
      title: 'Unit types',
      description: 'Define the levels of your organization without code.',
      new: 'New type',
      edit: 'Edit type',
      canBeRoot: 'Can be root',
      allowedParents: 'Allowed parents (comma separated codes)',
      order: 'Order',
    },
  },
};
