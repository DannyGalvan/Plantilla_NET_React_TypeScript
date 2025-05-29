import type { ModuleResponse } from "./ModuleResponse";
import type { RolOperationResponse } from "./RolOperationResponse";

export interface OperationResponse {
  id: number;
  guid: string;
  name: string;
  description: string;
  policy: string;
  icon: string;
  path: string;
  moduleId: number;
  isVisible: boolean;
  state: number;
  createdAt: string;
  updatedAt?: string;
  createdBy: number;
  updatedBy?: number;
  module?: ModuleResponse;
  rolOperations?: RolOperationResponse[];
}
