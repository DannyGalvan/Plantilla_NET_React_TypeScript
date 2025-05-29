import type { RolOperationResponse } from "./RolOperationResponse";

export interface RolResponse {
  id: number;
  name: string;
  description: string;
  state: number;
  createdAt: string;
  createdBy: number;
  updatedBy?: number;
  updatedAt?: string;
  rolOperations?: RolOperationResponse[];
}
