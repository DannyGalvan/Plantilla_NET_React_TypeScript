import type { Authorizations } from "./Authorizations";

export interface AuthResponse {
  name: string;
  userName: string;
  email: string;
  token: string;
  redirect: string;
  userId: string;
  operations: Authorizations[];
}

export type ApiResponse<T> =
  | {
      success: true;
      data: T;
      message?: string;
      totalResults: number;
    }
  | {
      success: false | null;
      data: null;
      message?: string;
      totalResults: number;
    };
