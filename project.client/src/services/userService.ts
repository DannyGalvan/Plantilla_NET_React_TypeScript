import { api } from "../configs/axios/interceptors";
import type { ApiResponse } from "../types/ApiResponse";
import type { filterOptions } from "../types/FilterTypes";
import type { UserRequest } from "../types/UserRequest";
import type { UserResponse } from "../types/UserResponse";

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

  return api.get<unknown, ApiResponse<UserResponse[]>>(baseQuery);
};

export const getUserById = async (id: number) => {
  return api.get<unknown, ApiResponse<UserResponse>>(`User/${id}?Include=rol`);
};

export const createUser = async (User: UserRequest) => {
  return api.post<unknown, ApiResponse<UserResponse>, UserRequest>(
    "User",
    User,
  );
};

export const updateUser = async (User: UserRequest) => {
  return api.put<unknown, ApiResponse<UserResponse>, UserRequest>(`User`, User);
};

export const deleteUser = async (id: number) => {
  return api.delete<unknown, ApiResponse<UserResponse>>(`User/${id}`);
};
