import type { OperationResponse } from "./OperationResponse";

export interface ModuleResponse {
  id: number;
  name: string;
  description: string;
  image: string;
  path: string;
  state: number;
  createdAt: string;
  updatedAt?: string;
  createdBy: number;
  updatedBy?: number;
  operations?: OperationResponse[];
}
