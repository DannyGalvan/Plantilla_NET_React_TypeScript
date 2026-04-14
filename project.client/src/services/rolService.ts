import { api } from "../configs/axios/interceptors";
import type { ApiResponse } from "../types/ApiResponse";
import type { filterOptions } from "../types/FilterTypes";
import type { RolRequest } from "../types/RolRequest";
import type { RolResponse } from "../types/RolResponse";

export const getRoles = async ({
  pageNumber = 1,
  pageSize = 10,
  filters,
  include,
  includeTotal = false,
}: filterOptions): Promise<ApiResponse<RolResponse[]>> => {
  let baseQuery = `Rol?pageNumber=${pageNumber}&pageSize=${pageSize}`;

  if (filters) {
    baseQuery += `&filters=${encodeURIComponent(filters)}`;
  }
  if (include) {
    baseQuery += `&include=${encodeURIComponent(include)}`;
  }
  if (includeTotal) {
    baseQuery += `&includeTotal=${includeTotal}`;
  }

  return api.get<unknown, ApiResponse<RolResponse[]>>(baseQuery);
};

export const getRolById = async (
  id: number,
): Promise<ApiResponse<RolResponse>> => {
  const response = await api.get<unknown, ApiResponse<RolResponse>>(
    `Rol/${id}`,
  );
  return response;
};

export const createRol = async (
  rol: RolRequest,
): Promise<ApiResponse<RolResponse>> => {
  const response = await api.post<unknown, ApiResponse<RolResponse>>(
    "Rol",
    rol,
  );
  return response;
};

export const updateRol = async (
  rol: RolRequest,
): Promise<ApiResponse<RolResponse>> => {
  const response = await api.put<unknown, ApiResponse<RolResponse>>("Rol", rol);
  return response;
};

export const deleteRol = async (
  id: number,
): Promise<ApiResponse<RolResponse>> => {
  const response = await api.delete<unknown, ApiResponse<RolResponse>>(
    `Rol/${id}`,
  );
  return response;
};
