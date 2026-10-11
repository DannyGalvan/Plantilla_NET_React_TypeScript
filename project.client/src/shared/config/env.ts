import { z } from 'zod';

const envSchema = z.object({
  VITE_API_URL: z.string().url().default('http://localhost:5050'),
  VITE_TENANCY_MODE: z
    .enum(['None', 'SharedDatabase', 'DatabasePerTenant', 'Hybrid'])
    .default('None'),
});

const parsed = envSchema.safeParse(import.meta.env);

if (!parsed.success) {
  console.warn('Invalid env', parsed.error.flatten());
}

export const env = parsed.success
  ? parsed.data
  : {
      VITE_API_URL: 'http://localhost:5050',
      VITE_TENANCY_MODE: 'None' as const,
    };
