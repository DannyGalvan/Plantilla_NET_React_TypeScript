import { z } from "zod";

import type { ApiResponse } from "../types/ApiResponse";
import type { filterOptions } from "../types/FilterTypes";
import type { OperationResponse } from "../types/OperationResponse";
import { operationResponseSchema } from "../types/schemas";

import { zodApi } from "./zodApi";

const operationListSchema = z.array(operationResponseSchema);

type OperationList = z.infer<typeof operationListSchema>;

export const getOperations = async ({
  pageNumber = 1,
  pageSize = 10,
  filters,
  include,
  includeTotal = false,
}: filterOptions): Promise<ApiResponse<OperationResponse[]>> => {
  let baseQuery = `Operation?pageNumber=${pageNumber}&pageSize=${pageSize}`;

  if (filters) {
    baseQuery += `&filters=${encodeURIComponent(filters)}`;
  }
  if (include) {
    baseQuery += `&include=${encodeURIComponent(include)}`;
  }
  if (includeTotal) {
    baseQuery += `&includeTotal=${includeTotal}`;
  }

  return zodApi.get<OperationList>(baseQuery, operationListSchema) as Promise<
    ApiResponse<OperationResponse[]>
  >;
};
