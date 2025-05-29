import type { Authorizations } from "./Authorizations";

export interface InitialAuth {
  isLoggedIn: boolean;
  email: string;
  redirect: boolean;
  userName: string;
  name: string;
  token: string;
  userId: number;
  operations: Authorizations[];
}
