import type { Authorizations } from "./Authorizations";

export interface LoginRequest {
  userName: string;
  password: string;
}

export interface LoginResponse {
  name: string;
  userName: string;
  email: string;
  token: string;
  redirect: boolean;
  userId: number;
  rol: number;
  operations: Authorizations[];
}
