import { z } from 'zod';

import { env } from '../../../shared/config/env';

const httpBase = env.VITE_API_URL;

export const productSchema = z.object({
  sku: z.string().min(1, 'Requerido'),
  name: z.string().min(1, 'Requerido'),
  description: z.string().optional(),
  price: z.coerce.number().min(0),
  stock: z.coerce.number().int().min(0),
  categoryId: z.string().min(1, 'Requerido'),
});

export type ProductInput = z.infer<typeof productSchema>;

export interface ProductDto {
  id: string;
  sku: string;
  name: string;
  description: string | null;
  price: number;
  stock: number;
  categoryId: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number | null;
}

export async function listProducts(
  page = 1,
  pageSize = 20,
): Promise<PagedResult<ProductDto>> {
  const res = await fetch(
    `${httpBase}/catalog/products?page=${page}&pageSize=${pageSize}`,
    {
      credentials: 'include',
    },
  );
  if (!res.ok) throw new Error(`listProducts ${res.status}`);
  return (await res.json()) as PagedResult<ProductDto>;
}

export async function createProduct(input: ProductInput): Promise<ProductDto> {
  const res = await fetch(`${httpBase}/catalog/products`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify(input),
  });
  if (!res.ok) throw new Error(`createProduct ${res.status}`);
  return (await res.json()) as ProductDto;
}

export async function deleteProduct(id: string): Promise<void> {
  const res = await fetch(`${httpBase}/catalog/products/${id}`, {
    method: 'DELETE',
    credentials: 'include',
  });
  if (!res.ok && res.status !== 404)
    throw new Error(`deleteProduct ${res.status}`);
}
