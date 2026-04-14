export type filterType =
  | "filters"
  | "include"
  | "pageNumber"
  | "pageSize"
  | "includeTotal";

export type filterOptions = {
  [key in filterType]: string | number | boolean | null | undefined;
};

export type filterByIdOptions = {
  id: string;
  includes?: string;
};

export interface filtersCatalogueOptions extends filterOptions {
  catalogue: string;
}

export interface filtersTraceability {
  jobRequisitionId: number;
  filters?: string;
}
