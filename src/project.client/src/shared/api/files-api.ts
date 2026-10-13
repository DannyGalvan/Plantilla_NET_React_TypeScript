import { z } from 'zod';

import { pagedSchema } from './crud-api';
import { http } from './http-client';

/** File metadata returned by the Files module (the content is downloaded separately). */
export const fileSchema = z.object({
  id: z.string(),
  fileName: z.string(),
  contentType: z.string(),
  length: z.number(),
  provider: z.string(),
  entityType: z.string().nullable(),
  entityId: z.string().nullable(),
  createdAt: z.string(),
  createdBy: z.string().nullable(),
});

export type StoredFile = z.infer<typeof fileSchema>;

const filePage = pagedSchema(fileSchema);

export interface FileQuery {
  entityType?: string;
  entityId?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}

/**
 * Files API shared by every module: library files (no entity) and attachments of a record
 * (`entityType` + `entityId`, declared by the owning module on the backend).
 */
export const filesApi = {
  list: async (query: FileQuery) => filePage.parse((await http.get('/files', { params: query })).data),
  upload: async (file: File, entityType?: string, entityId?: string) => {
    const form = new FormData();
    form.append('file', file);
    if (entityType && entityId) {
      form.append('entityType', entityType);
      form.append('entityId', entityId);
    }

    return fileSchema.parse((await http.post('/files', form)).data);
  },
  remove: async (id: string) => {
    await http.delete(`/files/${id}`);
  },
  /** Downloads through the authenticated client and hands the blob to the browser. */
  download: async (file: Pick<StoredFile, 'id' | 'fileName'>) => {
    const response = await http.get<Blob>(`/files/${file.id}/content`, { responseType: 'blob' });
    const url = URL.createObjectURL(response.data);
    const link = document.createElement('a');
    link.href = url;
    link.download = file.fileName;
    link.click();
    URL.revokeObjectURL(url);
  },
};

export function formatBytes(bytes: number): string {
  const units = ['B', 'KB', 'MB', 'GB'];
  let value = bytes;
  let unit = 0;
  while (value >= 1024 && unit < units.length - 1) {
    value /= 1024;
    unit++;
  }

  return `${value.toFixed(unit === 0 ? 0 : 1)} ${units[unit]}`;
}
