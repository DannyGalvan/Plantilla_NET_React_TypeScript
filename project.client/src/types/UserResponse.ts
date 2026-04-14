import type { RolResponse } from "./RolResponse";

export interface UserResponse {
  id: number;
  rolId: number;
  email: string;
  name: string;
  userName: string;
  number: string;
  state: number;
  createdAt: string;
  updatedAt?: string | null;
  createdBy: number;
  updatedBy?: number | null;

  rol?: RolResponse | null;
}
