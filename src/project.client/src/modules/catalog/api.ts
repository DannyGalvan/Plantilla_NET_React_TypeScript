import { z } from 'zod';

import { createCrudApi } from '@/shared/api/crud-api';
import { http } from '@/shared/api/http-client';

export const categorySchema = z.object({
  id: z.string(),
  name: z.string(),
  description: z.string().nullable(),
  createdAt: z.string(),
  concurrencyStamp: z.string(),
});

export const productSchema = z.object({
  id: z.string(),
  sku: z.string(),
  name: z.string(),
  description: z.string().nullable(),
  price: z.number(),
  stock: z.number(),
  categoryId: z.string(),
  organizationUnitId: z.string(),
  isActive: z.boolean(),
  createdAt: z.string(),
  concurrencyStamp: z.string(),
});

export const stockRowSchema = z.object({
  categoryId: z.string(),
  categoryName: z.string(),
  products: z.number(),
  unitsInStock: z.number(),
  inventoryValue: z.number(),
});

export const lowStockRowSchema = z.object({
  id: z.string(),
  sku: z.string(),
  name: z.string(),
  stock: z.number(),
  organizationUnitId: z.string(),
});

export type Category = z.infer<typeof categorySchema>;
export type Product = z.infer<typeof productSchema>;
export type StockRow = z.infer<typeof stockRowSchema>;

export interface CategoryInput {
  name: string;
  description?: string;
  concurrencyStamp?: string;
}

export interface ProductInput {
  sku?: string;
  name: string;
  description?: string;
  price: number;
  stock: number;
  categoryId: string;
  isActive?: boolean;
  organizationUnitId?: string;
  concurrencyStamp?: string;
}

/** Generic CRUD clients: one line per backend resource registered with MapCrud. */
export const categoriesApi = createCrudApi<typeof categorySchema, CategoryInput, CategoryInput>(
  '/catalog/categories',
  categorySchema,
);
export const productsApi = createCrudApi<typeof productSchema, ProductInput, ProductInput>(
  '/catalog/products',
  productSchema,
);

export const reportsApi = {
  stock: async () => z.array(stockRowSchema).parse((await http.get('/catalog/reports/stock')).data),
  lowStock: async (threshold: number) =>
    z.array(lowStockRowSchema).parse((await http.get('/catalog/reports/low-stock', { params: { threshold } })).data),
};
