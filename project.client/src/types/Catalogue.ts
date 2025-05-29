export interface Catalogue {
  id: string;
  name: string;
  description: string;
  state: boolean;
  createdAt: string;
  updatedAt?: string;
  createdBy: string;
  updatedBy?: string;
}

export interface SearchCatalogue {
  [key: string]: string | boolean | undefined | number | object | null;
}
