import { api } from "../configs/axios/interceptors";
import type { ApiResponse } from "../types/ApiResponse";
import type { filterOptions } from "../types/FilterTypes";
import type { RolOperationRequest } from "../types/RolOperationRequest";
import type { RolOperationResponse } from "../types/RolOperationResponse";

export const getRolOperations = async ({
  pageNumber = 1,
  pageSize = 10,
  filters,
  include,
  includeTotal = false,
}: filterOptions): Promise<ApiResponse<RolOperationResponse[]>> => {
  let baseQuery = `RolOperation?pageNumber=${pageNumber}&pageSize=${pageSize}`;

  if (filters) {
    baseQuery += `&filters=${encodeURIComponent(filters)}`;
  }
  if (include) {
    baseQuery += `&include=${encodeURIComponent(include)}`;
  }
  if (includeTotal) {
    baseQuery += `&includeTotal=${includeTotal}`;
  }

  return api.get<unknown, ApiResponse<RolOperationResponse[]>>(baseQuery);
};

export const getRolOperationById = async (id: number) => {
  return api.get<unknown, ApiResponse<RolOperationResponse>>(
    `RolOperation/${id}`,
  );
};

export const createRolOperation = async (rolOperation: RolOperationRequest) => {
  return api.post<
    unknown,
    ApiResponse<RolOperationResponse>,
    RolOperationRequest
  >("RolOperation", rolOperation);
};

export const updateRolOperation = async (rolOperation: RolOperationRequest) => {
  return api.put<
    unknown,
    ApiResponse<RolOperationResponse>,
    RolOperationRequest
  >(`RolOperation`, rolOperation);
};
