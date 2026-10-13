import { z } from 'zod';

import { http } from '@/shared/api/http-client';

export const stepSchema = z.object({
  name: z.string(),
  kind: z.enum(['Permission', 'Role', 'User']),
  approver: z.string(),
  minAmount: z.number().nullable().optional(),
});

export const requestSchema = z.object({
  id: z.string(),
  workflow: z.string(),
  entityType: z.string(),
  entityId: z.string(),
  title: z.string(),
  link: z.string().nullable(),
  amount: z.number().nullable(),
  requestedBy: z.string(),
  requestedAt: z.string(),
  status: z.enum(['Pending', 'Approved', 'Rejected', 'Cancelled']),
  currentStep: z.number(),
  steps: z.array(stepSchema),
  decisions: z.array(
    z.object({
      step: z.number(),
      decision: z.string(),
      userId: z.string(),
      comment: z.string().nullable(),
      at: z.string(),
    }),
  ),
  canDecide: z.boolean(),
});

export const workflowSchema = z.object({
  key: z.string(),
  module: z.string(),
  name: z.string(),
  defaultSteps: z.array(stepSchema),
  steps: z.array(stepSchema),
  customized: z.boolean(),
});

export type ApprovalStep = z.infer<typeof stepSchema>;
export type ApprovalRequest = z.infer<typeof requestSchema>;
export type Workflow = z.infer<typeof workflowSchema>;

export const workflowsApi = {
  inbox: async () => z.array(requestSchema).parse((await http.get('/workflows/requests/inbox')).data),
  mine: async () => z.array(requestSchema).parse((await http.get('/workflows/requests/mine')).data),
  approve: async (id: string, comment: string) => {
    await http.post(`/workflows/requests/${id}/approve`, { comment });
  },
  reject: async (id: string, comment: string) => {
    await http.post(`/workflows/requests/${id}/reject`, { comment });
  },
  cancel: async (id: string) => {
    await http.post(`/workflows/requests/${id}/cancel`);
  },
  definitions: async () => z.array(workflowSchema).parse((await http.get('/workflows/definitions')).data),
  updateDefinition: async (key: string, steps: ApprovalStep[] | null) => {
    await http.put(`/workflows/definitions/${key}`, { steps });
  },
};
