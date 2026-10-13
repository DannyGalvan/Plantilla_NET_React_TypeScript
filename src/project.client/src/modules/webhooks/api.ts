import { z } from 'zod';

import { http } from '@/shared/api/http-client';

export const subscriptionSchema = z.object({
  id: z.string(),
  name: z.string(),
  url: z.string(),
  events: z.array(z.string()),
  isActive: z.boolean(),
  createdAt: z.string(),
});

export const deliverySchema = z.object({
  id: z.string(),
  event: z.string(),
  status: z.enum(['Pending', 'Delivered', 'Failed']),
  attempts: z.number(),
  createdAt: z.string(),
  lastAttemptAt: z.string().nullable(),
  nextAttemptAt: z.string(),
  responseStatus: z.number().nullable(),
  lastError: z.string().nullable(),
});

export type Subscription = z.infer<typeof subscriptionSchema>;
export type Delivery = z.infer<typeof deliverySchema>;

export interface SubscriptionInput {
  name: string;
  url: string;
  events: string[];
  isActive: boolean;
}

export const webhooksApi = {
  events: async () =>
    z
      .array(z.object({ name: z.string(), description: z.string().nullable() }))
      .parse((await http.get('/webhooks/events')).data),
  subscriptions: async () => z.array(subscriptionSchema).parse((await http.get('/webhooks/subscriptions')).data),
  create: async (input: SubscriptionInput) =>
    z
      .object({ subscription: subscriptionSchema, secret: z.string() })
      .parse((await http.post('/webhooks/subscriptions', input)).data),
  update: async (id: string, input: SubscriptionInput) =>
    subscriptionSchema.parse((await http.put(`/webhooks/subscriptions/${id}`, input)).data),
  remove: async (id: string) => {
    await http.delete(`/webhooks/subscriptions/${id}`);
  },
  ping: async (id: string) => {
    await http.post(`/webhooks/subscriptions/${id}/ping`);
  },
  deliveries: async (id: string) =>
    z.array(deliverySchema).parse((await http.get(`/webhooks/subscriptions/${id}/deliveries`)).data),
  retry: async (id: string) => {
    await http.post(`/webhooks/deliveries/${id}/retry`);
  },
};
