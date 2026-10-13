import { z } from 'zod';

import { http } from '@/shared/api/http-client';

export const jobSchema = z.object({
  name: z.string(),
  description: z.string().nullable(),
  scope: z.string(),
  defaultCron: z.string(),
  cron: z.string().nullable(),
  enabled: z.boolean(),
  nextRunAt: z.string().nullable(),
  lastStartedAt: z.string().nullable(),
  lastFinishedAt: z.string().nullable(),
  lastStatus: z.string().nullable(),
  lastError: z.string().nullable(),
});

export const jobRunSchema = z.object({
  id: z.string(),
  name: z.string(),
  startedAt: z.string(),
  finishedAt: z.string(),
  status: z.string(),
  error: z.string().nullable(),
});

export type Job = z.infer<typeof jobSchema>;
export type JobRun = z.infer<typeof jobRunSchema>;

export const jobsApi = {
  list: async () => z.array(jobSchema).parse((await http.get('/jobs')).data),
  update: async (name: string, enabled: boolean, cron: string | null) => {
    await http.put(`/jobs/${name}`, { enabled, cron });
  },
  run: async (name: string) => {
    await http.post(`/jobs/${name}/run`);
  },
  runs: async (name: string) => z.array(jobRunSchema).parse((await http.get(`/jobs/${name}/runs`)).data),
};
