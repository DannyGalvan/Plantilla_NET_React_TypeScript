import { z } from 'zod';

import { http } from '@/shared/api/http-client';

export const sequenceSchema = z.object({
  name: z.string(),
  description: z.string().nullable(),
  defaultFormat: z.string(),
  format: z.string(),
  reset: z.string(),
  perOrganizationUnit: z.boolean(),
  example: z.string(),
});

export type Sequence = z.infer<typeof sequenceSchema>;

export const sequencesApi = {
  list: async () => z.array(sequenceSchema).parse((await http.get('/sequences')).data),
  update: async (name: string, format: string | null) => {
    await http.put(`/sequences/${name}`, { format });
  },
};
