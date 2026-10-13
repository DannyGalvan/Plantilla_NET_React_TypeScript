import { z } from 'zod';

import { pagedSchema, type ListParams } from '@/shared/api/crud-api';
import { http } from '@/shared/api/http-client';

export const tenantSchema = z.object({
  id: z.string(),
  code: z.string(),
  name: z.string(),
  status: z.enum(['Active', 'Suspended', 'Provisioning', 'Archived']),
  isolation: z.enum(['Shared', 'Dedicated']),
  databaseProvider: z.string().nullable(),
  plan: z.string().nullable(),
  hasDedicatedConnection: z.boolean(),
  domains: z.array(z.object({ id: z.string(), host: z.string(), isPrimary: z.boolean() })),
  features: z.array(z.object({ feature: z.string(), enabled: z.boolean() })),
  createdAt: z.string(),
  concurrencyStamp: z.string(),
});

const jsonObject = z.record(z.string(), z.unknown());

export const settingsSectionSchema = z.object({
  key: z.string(),
  allowUserOverride: z.boolean(),
  defaults: jsonObject,
});
export const settingsValueSchema = z.object({
  section: z.string(),
  scope: z.enum(['System', 'Tenant', 'OrganizationUnit', 'User']),
  scopeId: z.string().nullable(),
  overrides: jsonObject,
  effective: jsonObject,
});

export type Tenant = z.infer<typeof tenantSchema>;
export type SettingsSection = z.infer<typeof settingsSectionSchema>;
export type SettingScope = z.infer<typeof settingsValueSchema>['scope'];

export interface CreateTenantInput {
  code: string;
  name: string;
  isolation: 'Shared' | 'Dedicated';
  databaseProvider?: string;
  connectionSecretRef?: string;
  plan?: string;
  primaryDomain?: string;
}

const tenantsPage = pagedSchema(tenantSchema);

export const tenancyApi = {
  list: async (params: ListParams) => tenantsPage.parse((await http.get('/tenancy/tenants', { params })).data),
  create: async (input: CreateTenantInput) => tenantSchema.parse((await http.post('/tenancy/tenants', input)).data),
  setStatus: async (id: string, status: Tenant['status']) =>
    tenantSchema.parse((await http.post(`/tenancy/tenants/${id}/status`, { status })).data),
  addDomain: async (id: string, host: string) =>
    tenantSchema.parse((await http.post(`/tenancy/tenants/${id}/domains`, { host, isPrimary: false })).data),
  removeDomain: async (id: string, domainId: string) =>
    tenantSchema.parse((await http.delete(`/tenancy/tenants/${id}/domains/${domainId}`)).data),
  setFeature: async (id: string, feature: string, enabled: boolean) =>
    tenantSchema.parse((await http.put(`/tenancy/tenants/${id}/features/${feature}`, { enabled })).data),
  provision: async (id: string) => tenantSchema.parse((await http.post(`/tenancy/tenants/${id}/provision`)).data),

  settingsSchema: async () => z.array(settingsSectionSchema).parse((await http.get('/tenancy/settings/schema')).data),
  getSettings: async (section: string, scope: SettingScope, scopeId?: string) =>
    settingsValueSchema.parse((await http.get(`/tenancy/settings/${section}`, { params: { scope, scopeId } })).data),
  updateSettings: async (section: string, scope: SettingScope, values: Record<string, unknown>, scopeId?: string) =>
    settingsValueSchema.parse(
      (await http.put(`/tenancy/settings/${section}`, values, { params: { scope, scopeId } })).data,
    ),
};
