import { z } from 'zod';

import { pagedSchema } from './crud-api';
import { http } from './http-client';

export const auditEntrySchema = z.object({
  id: z.string(),
  entityType: z.string(),
  entityId: z.string(),
  operation: z.enum(['created', 'updated', 'deleted', 'restored']),
  changes: z.array(
    z.object({ property: z.string(), oldValue: z.string().nullable(), newValue: z.string().nullable() }),
  ),
  userId: z.string().nullable(),
  userName: z.string().nullable(),
  occurredAt: z.string(),
});

export type AuditEntry = z.infer<typeof auditEntrySchema>;

const page = pagedSchema(auditEntrySchema);

export interface AuditQuery {
  entityType?: string;
  entityId?: string;
  userId?: string;
  page?: number;
  pageSize?: number;
}

/** Change history recorded by the Audit module for every module (permission `audit.history.view`). */
export const auditApi = {
  list: async (query: AuditQuery) => page.parse((await http.get('/audit/entries', { params: query })).data),
};
