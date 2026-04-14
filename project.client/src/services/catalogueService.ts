import { api } from "../configs/axios/interceptors";
import type { ApiResponse } from "../types/ApiResponse";
import type { CatalogueResponse } from "../types/CatalogueResponse";
import type { filterOptions } from "../types/FilterTypes";

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
  let baseQuery = `Catalogue/${catalogue}?pageNumber=${pageNumber}&pageSize=${pageSize}`;

  if (filters) {
    baseQuery += `&filters=${encodeURIComponent(filters)}`;
  }
  if (include) {
    baseQuery += `&include=${encodeURIComponent(include)}`;
  }
  if (includeTotal) {
    baseQuery += `&includeTotal=${includeTotal}`;
  }

  return api.get<unknown, ApiResponse<CatalogueResponse[]>>(baseQuery);
};
