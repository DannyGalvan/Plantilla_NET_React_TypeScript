import { z } from 'zod';

const schema = z.object({
  VITE_API_BASE_URL: z.string().default('/api/v1'),
  MODE: z.string(),
  DEV: z.boolean(),
});

/** Validated build-time environment. Brand values are NOT here: they come from /platform/config at runtime. */
export const env = schema.parse(import.meta.env);
