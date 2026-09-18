import { z } from "zod";

import type { ApiResponse } from "../types/ApiResponse";
import type { filterOptions } from "../types/FilterTypes";
import type { UserRequest } from "../types/UserRequest";
import type { UserResponse } from "../types/UserResponse";
import { userResponseSchema } from "../types/schemas";

import { zodApi } from "./zodApi";

/** Schema for `GET /User?…` — returns an array of users. */
const userListSchema = z.array(userResponseSchema);
/** Schema for `GET /User/{id}?Include=rol` — returns a single user. */
const userSchema = userResponseSchema;

type UserList = z.infer<typeof userListSchema>;

export const getUsers = async ({
  pageNumber = 1,
  pageSize = 10,
  filters,
  include,
  includeTotal = false,
}: filterOptions): Promise<ApiResponse<UserResponse[]>> => {
  let baseQuery = `User?pageNumber=${pageNumber}&pageSize=${pageSize}`;

  if (filters) {
    baseQuery += `&filters=${encodeURIComponent(filters)}`;
  }
  if (include) {
    baseQuery += `&include=${encodeURIComponent(include)}`;
  }
  if (includeTotal) {
    baseQuery += `&includeTotal=${includeTotal}`;
  }

  return zodApi.get<UserList>(baseQuery, userListSchema) as Promise<
    ApiResponse<UserResponse[]>
  >;
};

export const getUserById = async (
  id: number,
): Promise<ApiResponse<UserResponse>> => {
  const env = await zodApi.get<z.infer<typeof userSchema>>(
    `User/${id}?Include=rol`,
    userSchema,
  );
  return env as ApiResponse<UserResponse>;
};

export const createUser = async (
  user: UserRequest,
): Promise<ApiResponse<UserResponse>> => {
  const env = await zodApi.post<z.infer<typeof userSchema>, UserRequest>(
    "User",
    userSchema,
    user,
  );
  return env as ApiResponse<UserResponse>;
};

export const updateUser = async (
  user: UserRequest,
): Promise<ApiResponse<UserResponse>> => {
  const env = await zodApi.put<z.infer<typeof userSchema>, UserRequest>(
    "User",
    userSchema,
    user,
  );
  return env as ApiResponse<UserResponse>;
};

export const deleteUser = async (
  id: number,
): Promise<ApiResponse<UserResponse>> => {
  const env = await zodApi.delete<z.infer<typeof userSchema>>(
    `User/${id}`,
    userSchema,
  );
  return env as ApiResponse<UserResponse>;
};
