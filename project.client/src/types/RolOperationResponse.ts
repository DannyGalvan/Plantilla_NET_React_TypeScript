import type { OperationResponse } from "./OperationResponse";
import type { RolResponse } from "./RolResponse";

export interface RolOperationResponse {
  id: number;
  rolId: number;
  operationId: number;
  state: number;
  createdAt: string;
  createdBy: number;
  updatedBy?: number;
  updatedAt?: string;
  rol?: RolResponse;
  operation?: OperationResponse;
}
