export interface RolOperationRequest {
  id: number | null;
  rolId: number;
  operationId: number;
  state: number;
  createdAt?: string;
  updatedAt?: string;
}
