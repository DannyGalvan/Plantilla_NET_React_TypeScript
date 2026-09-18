import { z } from "zod";

import type { ApiResponse } from "../types/ApiResponse";
import type { CatalogueResponse } from "../types/CatalogueResponse";
import type { filterOptions } from "../types/FilterTypes";
import { catalogueResponseItemSchema } from "../types/schemas";

import { zodApi } from "./zodApi";

const catalogueListSchema = z.array(catalogueResponseItemSchema);

interface FiltersCatalogue extends filterOptions {
  catalogue: string;
}

export const getCatalogue = async ({
  pageNumber = 1,
  pageSize = 10,
  filters,
  include,
  includeTotal = false,
  catalogue,
}: FiltersCatalogue): Promise<ApiResponse<CatalogueResponse[]>> => {
  // F12: encode the path segment too, not just the query string. Defence in
  // depth — the server rejects unknown paths, but an attacker who slipped a
  // `../` into `catalogue` would otherwise hit a different URL space.
  let baseQuery = `Catalogue/${encodeURIComponent(catalogue)}?pageNumber=${pageNumber}&pageSize=${pageSize}`;

  if (filters) {
    baseQuery += `&filters=${encodeURIComponent(filters)}`;
  }
  if (include) {
    baseQuery += `&include=${encodeURIComponent(include)}`;
  }
  if (includeTotal) {
    baseQuery += `&includeTotal=${includeTotal}`;
  }

  return zodApi.get(baseQuery, catalogueListSchema) as Promise<
    ApiResponse<CatalogueResponse[]>
  >;
};
