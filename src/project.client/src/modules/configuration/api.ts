import { z } from 'zod';

import { http } from '@/shared/api/http-client';

export const fieldSchema = z.object({
  key: z.string(),
  kind: z.enum(['Text', 'Number', 'Boolean', 'List', 'Map']),
  secret: z.boolean(),
  requiresRestart: z.boolean(),
  help: z.string().nullable(),
  default: z.unknown().nullable(),
  value: z.unknown().nullable(),
  hasValue: z.boolean(),
  source: z.enum(['database', 'environment', 'file', 'other']).nullable(),
});

export const sectionSchema = z.object({
  section: z.string(),
  module: z.string(),
  description: z.string().nullable(),
  fields: z.array(fieldSchema),
});

export const overviewSchema = z.object({ secretsAvailable: z.boolean(), sections: z.array(sectionSchema) });

export type ConfigField = z.infer<typeof fieldSchema>;
export type ConfigSection = z.infer<typeof sectionSchema>;

export const configurationApi = {
  get: async () => overviewSchema.parse((await http.get('/configuration')).data),
  /** `null` removes a value (the default applies); maps only change the names they contain. */
  update: async (section: string, values: Record<string, unknown>) => {
    await http.put(`/configuration/${section}`, { values });
  },
};
