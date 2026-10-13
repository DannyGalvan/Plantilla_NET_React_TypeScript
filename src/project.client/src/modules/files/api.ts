import { z } from 'zod';

import { http } from '@/shared/api/http-client';

export const providerSchema = z.object({ provider: z.string(), usesOAuth: z.boolean(), available: z.boolean() });

export const connectionSchema = z.object({
  id: z.string(),
  name: z.string(),
  provider: z.string(),
  account: z.string().nullable(),
  isDefault: z.boolean(),
  settings: z.record(z.string(), z.string()),
  createdAt: z.string(),
});

export type StorageProvider = z.infer<typeof providerSchema>;
export type StorageConnection = z.infer<typeof connectionSchema>;

export interface CreateConnectionInput {
  name: string;
  provider: string;
  settings: Record<string, string>;
  credentials: Record<string, string>;
  isDefault: boolean;
}

/** Storage administration of the tenant (permission `files.storage.manage`). */
export const storageApi = {
  providers: async () => z.array(providerSchema).parse((await http.get('/files/storage/providers')).data),
  connections: async () => z.array(connectionSchema).parse((await http.get('/files/storage/connections')).data),
  create: async (input: CreateConnectionInput) =>
    connectionSchema.parse((await http.post('/files/storage/connections', input)).data),
  remove: async (id: string) => {
    await http.delete(`/files/storage/connections/${id}`);
  },
  setDefault: async (connectionId: string | null) => {
    await http.put('/files/storage/default', { connectionId });
  },
  /** Returns the consent URL of Google or Microsoft; the browser is sent there. */
  startOAuth: async (provider: string, name: string, isDefault: boolean) =>
    z
      .object({ url: z.string() })
      .parse((await http.post(`/files/storage/oauth/${provider}/start`, { name, isDefault })).data).url,
};

/** Fields of the key-based providers: non-secret settings and write-only credentials. */
export const keyProviders: Record<string, { settings: string[]; credentials: string[] }> = {
  s3: { settings: ['bucket', 'region', 'prefix'], credentials: ['accessKeyId', 'secretAccessKey'] },
  'azure-blob': { settings: ['container'], credentials: ['connectionString'] },
};
