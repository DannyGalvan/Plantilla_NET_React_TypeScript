import type { ModuleResources } from '@/shared/i18n';

export const resources: ModuleResources = {
  es: {
    title: 'Tareas programadas',
    description:
      'Procesos automáticos de los módulos. Se ejecutan en la zona horaria de tu organización; puedes pausarlos o cambiar su horario.',
    schedule: 'Horario (cron)',
    scheduleHint: 'minuto hora día mes día-semana. Vacío usa el horario original: {{cron}}',
    nextRun: 'Próxima ejecución',
    lastRun: 'Última ejecución',
    enabled: 'Activa',
    runNow: 'Ejecutar ahora',
    queued: 'La tarea se ejecutará en unos segundos.',
    history: 'Historial',
    noRuns: 'Aún no se ha ejecutado.',
    statuses: { succeeded: 'Correcta', failed: 'Con error' },
  },
  en: {
    title: 'Scheduled jobs',
    description:
      'Automatic processes of the modules. They run in the time zone of your organization; you can pause them or change their schedule.',
    schedule: 'Schedule (cron)',
    scheduleHint: 'minute hour day month weekday. Empty uses the default schedule: {{cron}}',
    nextRun: 'Next run',
    lastRun: 'Last run',
    enabled: 'Enabled',
    runNow: 'Run now',
    queued: 'The job will run in a few seconds.',
    history: 'History',
    noRuns: 'It has not run yet.',
    statuses: { succeeded: 'Succeeded', failed: 'Failed' },
  },
};
