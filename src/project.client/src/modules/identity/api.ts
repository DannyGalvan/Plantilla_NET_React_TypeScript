import { z } from 'zod';

import { pagedSchema, type ListParams } from '@/shared/api/crud-api';
import { http } from '@/shared/api/http-client';

export const roleRefSchema = z.object({ id: z.string(), name: z.string() });

export const userSchema = z.object({
  id: z.string(),
  userName: z.string(),
  email: z.string(),
  displayName: z.string().nullable(),
  isActive: z.boolean(),
  isLockedOut: z.boolean(),
  mustChangePassword: z.boolean(),
  lastLoginAt: z.string().nullable(),
  createdAt: z.string(),
  roles: z.array(roleRefSchema),
  assignments: z.array(
    z.object({ id: z.string(), roleId: z.string(), roleName: z.string(), organizationUnitId: z.string().nullable() }),
  ),
  concurrencyStamp: z.string(),
});

export const roleSchema = z.object({
  id: z.string(),
  name: z.string(),
  description: z.string().nullable(),
  isSystem: z.boolean(),
  isPlatform: z.boolean(),
  permissions: z.array(z.string()),
  userCount: z.number(),
  createdAt: z.string(),
  concurrencyStamp: z.string(),
});

export const permissionGroupSchema = z.object({
  module: z.string(),
  permissions: z.array(
    z.object({ key: z.string(), module: z.string(), displayKey: z.string(), group: z.string().nullable() }),
  ),
});

export type User = z.infer<typeof userSchema>;
export type Role = z.infer<typeof roleSchema>;
export type PermissionGroup = z.infer<typeof permissionGroupSchema>;

export interface CreateUserInput {
  userName: string;
  email: string;
  displayName?: string;
  password: string;
  roleIds: string[];
  mustChangePassword: boolean;
}

export interface UpdateUserInput {
  email: string;
  displayName?: string;
  isActive: boolean;
  roleIds: string[];
  concurrencyStamp: string;
}

const usersPage = pagedSchema(userSchema);
const rolesPage = pagedSchema(roleSchema);

export const identityApi = {
  users: {
    list: async (params: ListParams) => usersPage.parse((await http.get('/identity/users', { params })).data),
    create: async (input: CreateUserInput) => userSchema.parse((await http.post('/identity/users', input)).data),
    update: async (id: string, input: UpdateUserInput) =>
      userSchema.parse((await http.put(`/identity/users/${id}`, input)).data),
    remove: async (id: string) => {
      await http.delete(`/identity/users/${id}`);
    },
    unlock: async (id: string) => {
      await http.post(`/identity/users/${id}/unlock`);
    },
    resetPassword: async (id: string, newPassword: string) => {
      await http.post(`/identity/users/${id}/reset-password`, { newPassword });
    },
  },
  roles: {
    list: async (params: ListParams) => rolesPage.parse((await http.get('/identity/roles', { params })).data),
    all: async () => rolesPage.parse((await http.get('/identity/roles', { params: { pageSize: 100 } })).data).items,
    create: async (input: { name: string; description?: string }) =>
      roleSchema.parse((await http.post('/identity/roles', input)).data),
    update: async (id: string, input: { name: string; description?: string; concurrencyStamp: string }) =>
      roleSchema.parse((await http.put(`/identity/roles/${id}`, input)).data),
    remove: async (id: string) => {
      await http.delete(`/identity/roles/${id}`);
    },
    setPermissions: async (id: string, permissions: string[]) =>
      roleSchema.parse((await http.put(`/identity/roles/${id}/permissions`, { permissions })).data),
  },
  permissions: async () => z.array(permissionGroupSchema).parse((await http.get('/identity/permissions')).data),
};
