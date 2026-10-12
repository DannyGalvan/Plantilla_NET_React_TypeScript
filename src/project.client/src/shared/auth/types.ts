import { z } from 'zod';

export const tenantSummarySchema = z.object({
  id: z.string(),
  code: z.string(),
  name: z.string(),
  isDefault: z.boolean(),
});

export const profileSchema = z.object({
  id: z.string(),
  userName: z.string(),
  email: z.string(),
  displayName: z.string().nullable(),
  mustChangePassword: z.boolean(),
  tenant: tenantSummarySchema.nullable(),
  organizationUnitId: z.string().nullable(),
  roles: z.array(z.string()),
  permissions: z.array(z.string()),
  tenants: z.array(tenantSummarySchema),
  permissionsVersion: z.number(),
  isPlatformAdmin: z.boolean(),
});

export const sessionSchema = z.object({
  accessToken: z.string(),
  expiresAt: z.string(),
  mustChangePassword: z.boolean(),
  tenantId: z.string(),
  organizationUnitId: z.string().nullable(),
});

export const navigationItemSchema = z.object({
  key: z.string(),
  module: z.string(),
  labelKey: z.string(),
  path: z.string(),
  icon: z.string(),
  order: z.number(),
});

export type Profile = z.infer<typeof profileSchema>;
export type Session = z.infer<typeof sessionSchema>;
export type TenantSummary = z.infer<typeof tenantSummarySchema>;
export type NavigationItem = z.infer<typeof navigationItemSchema>;
