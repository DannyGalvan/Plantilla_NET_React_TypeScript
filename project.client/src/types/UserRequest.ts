import type { RolResponse } from "./RolResponse";

export interface UserRequest {
  id?: number | null;
  rolId?: number | null;
  email?: string | null;
  name?: string | null;
  userName?: string | null;
  password?: string | null;
  identificationDocument?: string | null;
  number?: string | null;
  state?: number | null;
  createdBy?: number | null;
  updatedBy?: number | null;

  rol?: RolResponse | null;
}
