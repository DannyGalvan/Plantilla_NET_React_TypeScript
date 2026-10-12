import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
  type QueryKey,
  type UseQueryOptions,
} from '@tanstack/react-query';
import { z } from 'zod';

import { http } from './http-client';

/** Query string accepted by every list endpoint (see the backend MapCrud). */
export interface ListParams {
  page?: number;
  pageSize?: number;
  sort?: string;
  filter?: string;
}

export const pagedSchema = <T extends z.ZodType>(item: T) =>
  z.object({
    items: z.array(item),
    page: z.number(),
    pageSize: z.number(),
    total: z.number().nullable(),
    totalPages: z.number().nullable(),
  });

export type Paged<T> = { items: T[]; page: number; pageSize: number; total: number | null; totalPages: number | null };

/**
 * Typed client for a backend CRUD resource. Responses are validated with zod, so the TypeScript types are
 * always `z.infer` of the schema (single source of truth).
 */
export function createCrudApi<TSchema extends z.ZodType, TCreate, TUpdate>(resource: string, schema: TSchema) {
  const page = pagedSchema(schema);

  return {
    resource,
    list: async (params: ListParams) => page.parse((await http.get(resource, { params })).data),
    get: async (id: string) => schema.parse((await http.get(`${resource}/${id}`)).data),
    create: async (input: TCreate) => schema.parse((await http.post(resource, input)).data),
    update: async (id: string, input: TUpdate) => schema.parse((await http.put(`${resource}/${id}`, input)).data),
    patch: async (id: string, changes: Partial<TUpdate>) =>
      schema.parse((await http.patch(`${resource}/${id}`, changes)).data),
    remove: async (id: string) => {
      await http.delete(`${resource}/${id}`);
    },
    restore: async (id: string) => {
      await http.post(`${resource}/${id}/restore`);
    },
  };
}

export type CrudApi<T, TCreate, TUpdate> = {
  resource: string;
  list: (params: ListParams) => Promise<Paged<T>>;
  get: (id: string) => Promise<T>;
  create: (input: TCreate) => Promise<T>;
  update: (id: string, input: TUpdate) => Promise<T>;
  remove: (id: string) => Promise<void>;
};

/** Paged list bound to the URL parameters; keeps the previous page visible while the next one loads. */
export function useCrudList<T>(
  key: QueryKey,
  api: Pick<CrudApi<T, unknown, unknown>, 'list'>,
  params: ListParams,
  options?: Partial<UseQueryOptions<Paged<T>>>,
) {
  return useQuery({
    queryKey: [...key, 'list', params],
    queryFn: () => api.list(params),
    placeholderData: keepPreviousData,
    ...options,
  });
}

/** Create, update and delete with automatic invalidation of the resource queries. */
export function useCrudMutations<T, TCreate, TUpdate>(key: QueryKey, api: CrudApi<T, TCreate, TUpdate>) {
  const client = useQueryClient();
  const invalidate = () => client.invalidateQueries({ queryKey: key });

  return {
    create: useMutation({ mutationFn: api.create, onSuccess: invalidate }),
    update: useMutation({
      mutationFn: ({ id, input }: { id: string; input: TUpdate }) => api.update(id, input),
      onSuccess: invalidate,
    }),
    remove: useMutation({ mutationFn: api.remove, onSuccess: invalidate }),
  };
}
