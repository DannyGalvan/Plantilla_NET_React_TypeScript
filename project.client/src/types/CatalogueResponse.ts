export interface CatalogueResponse {
  id: number;
  name: string;
  state: number;
  createdBy: number;
  updatedBy?: number | null;
  createdAt: string;
  updatedAt?: string | null;
}
