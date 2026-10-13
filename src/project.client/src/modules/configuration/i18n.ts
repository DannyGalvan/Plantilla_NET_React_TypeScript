import type { ModuleResources } from '@/shared/i18n';

export const resources: ModuleResources = {
  es: {
    title: 'Configuración de la plataforma',
    description:
      'Valores que la aplicación lee en tiempo de ejecución, guardados en la base de datos. Los cambios se aplican sin volver a desplegar.',
    noMasterKey:
      'No hay llave maestra (Configuration:MasterKey): puedes cambiar valores normales, pero no guardar secretos. Configúrala como variable de entorno y reinicia.',
    secretSet: 'Configurado (oculto)',
    secretEmpty: 'Sin configurar',
    secretHint: 'Escribe un valor nuevo para reemplazarlo. Nunca se muestra el actual.',
    listHint: 'Un valor por línea.',
    addEntry: 'Agregar',
    name: 'Nombre',
    value: 'Valor',
    reset: 'Usar valor por defecto',
    restart: 'Requiere reiniciar',
    envOverride: 'Una variable de entorno define este valor y tiene prioridad sobre lo que guardes aquí.',
    defaultValue: 'Por defecto: {{value}}',
    saved: 'Configuración guardada. Ya está aplicada.',
    sources: {
      database: 'Base de datos',
      environment: 'Variable de entorno',
      file: 'appsettings',
      other: 'Otro origen',
    },
    defaultSource: 'Por defecto',
  },
  en: {
    title: 'Platform configuration',
    description: 'Values the application reads at runtime, stored in the database. Changes apply without redeploying.',
    noMasterKey:
      'There is no master key (Configuration:MasterKey): you can change regular values but not store secrets. Set it as an environment variable and restart.',
    secretSet: 'Set (hidden)',
    secretEmpty: 'Not set',
    secretHint: 'Type a new value to replace it. The current one is never shown.',
    listHint: 'One value per line.',
    addEntry: 'Add',
    name: 'Name',
    value: 'Value',
    reset: 'Use default value',
    restart: 'Requires restart',
    envOverride: 'An environment variable sets this value and wins over what you save here.',
    defaultValue: 'Default: {{value}}',
    saved: 'Configuration saved. It is already applied.',
    sources: { database: 'Database', environment: 'Environment variable', file: 'appsettings', other: 'Other source' },
    defaultSource: 'Default',
  },
};
