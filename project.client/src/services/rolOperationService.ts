import { z } from "zod";

import type { ApiResponse } from "../types/ApiResponse";
import type { filterOptions } from "../types/FilterTypes";
import type { RolOperationRequest } from "../types/RolOperationRequest";
import type { RolOperationResponse } from "../types/RolOperationResponse";
import { rolOperationResponseSchema } from "../types/schemas";

import { zodApi } from "./zodApi";

const rolOperationListSchema = z.array(rolOperationResponseSchema);
const rolOperationSchema = rolOperationResponseSchema;

type RolOperationList = z.infer<typeof rolOperationListSchema>;
type RolOperation = z.infer<typeof rolOperationSchema>;

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

  return zodApi.get<RolOperationList>(
    baseQuery,
    rolOperationListSchema,
  ) as Promise<ApiResponse<RolOperationResponse[]>>;
};

export const getRolOperationById = async (
  id: number,
): Promise<ApiResponse<RolOperationResponse>> =>
  zodApi.get<RolOperation>(`RolOperation/${id}`, rolOperationSchema) as Promise<
    ApiResponse<RolOperationResponse>
  >;

export const createRolOperation = async (
  rolOperation: RolOperationRequest,
): Promise<ApiResponse<RolOperationResponse>> =>
  zodApi.post<RolOperation, RolOperationRequest>(
    "RolOperation",
    rolOperationSchema,
    rolOperation,
  ) as Promise<ApiResponse<RolOperationResponse>>;

export const updateRolOperation = async (
  rolOperation: RolOperationRequest,
): Promise<ApiResponse<RolOperationResponse>> =>
  zodApi.put<RolOperation, RolOperationRequest>(
    "RolOperation",
    rolOperationSchema,
    rolOperation,
  ) as Promise<ApiResponse<RolOperationResponse>>;
