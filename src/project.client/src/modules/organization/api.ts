import { z } from 'zod';

import { createCrudApi } from '@/shared/api/crud-api';
import { http } from '@/shared/api/http-client';

export const unitSchema = z.object({
  id: z.string(),
  typeCode: z.string(),
  parentId: z.string().nullable(),
  depth: z.number(),
  code: z.string(),
  name: z.string(),
  countryCode: z.string().nullable(),
  isActive: z.boolean(),
  createdAt: z.string(),
  concurrencyStamp: z.string(),
});

export const unitTypeSchema = z.object({
  id: z.string(),
  code: z.string(),
  name: z.string(),
  canBeRoot: z.boolean(),
  allowedParentCodes: z.string(),
  sortOrder: z.number(),
  concurrencyStamp: z.string(),
});

export const userSummarySchema = z.object({
  id: z.string(),
  userName: z.string(),
  displayName: z.string().nullable(),
  email: z.string(),
});

export type Unit = z.infer<typeof unitSchema>;
export type UnitType = z.infer<typeof unitTypeSchema>;

export interface UnitTypeInput {
  code?: string;
  name: string;
  canBeRoot: boolean;
  allowedParentCodes: string;
  sortOrder: number;
  concurrencyStamp?: string;
}

export const unitTypesApi = createCrudApi<typeof unitTypeSchema, UnitTypeInput, UnitTypeInput>(
  '/organization/unit-types',
  unitTypeSchema,
);

export const unitsApi = {
  list: async (search?: string) =>
    z.array(unitSchema).parse((await http.get('/organization/units', { params: { search } })).data),
  create: async (input: {
    typeCode: string;
    parentId: string | null;
    code: string;
    name: string;
    countryCode?: string;
  }) => unitSchema.parse((await http.post('/organization/units', input)).data),
  update: async (
    id: string,
    input: { code: string; name: string; countryCode?: string; isActive: boolean; concurrencyStamp: string },
  ) => unitSchema.parse((await http.put(`/organization/units/${id}`, input)).data),
  move: async (id: string, newParentId: string | null) =>
    unitSchema.parse((await http.post(`/organization/units/${id}/move`, { newParentId })).data),
  remove: async (id: string) => {
    await http.delete(`/organization/units/${id}`);
  },
  users: async (id: string) =>
    z.array(userSummarySchema).parse((await http.get(`/organization/units/${id}/users`)).data),
  assign: async (id: string, userId: string) => {
    await http.post(`/organization/units/${id}/users`, { userId });
  },
  unassign: async (id: string, userId: string) => {
    await http.delete(`/organization/units/${id}/users/${userId}`);
  },
};
