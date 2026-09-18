import { z } from "zod";

import type { ApiResponse } from "../types/ApiResponse";
import type { filterOptions } from "../types/FilterTypes";
import type { RolRequest } from "../types/RolRequest";
import type { RolResponse } from "../types/RolResponse";
import { rolResponseSchema } from "../types/schemas";

import { zodApi } from "./zodApi";

const rolListSchema = z.array(rolResponseSchema);
const rolSchema = rolResponseSchema;

type RolList = z.infer<typeof rolListSchema>;
type Rol = z.infer<typeof rolSchema>;

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

  return zodApi.get<RolList>(baseQuery, rolListSchema) as Promise<
    ApiResponse<RolResponse[]>
  >;
};

export const getRolById = async (
  id: number,
): Promise<ApiResponse<RolResponse>> =>
  zodApi.get<Rol>(`Rol/${id}`, rolSchema) as Promise<ApiResponse<RolResponse>>;

export const createRol = async (
  rol: RolRequest,
): Promise<ApiResponse<RolResponse>> =>
  zodApi.post<Rol, RolRequest>("Rol", rolSchema, rol) as Promise<
    ApiResponse<RolResponse>
  >;

export const updateRol = async (
  rol: RolRequest,
): Promise<ApiResponse<RolResponse>> =>
  zodApi.put<Rol, RolRequest>("Rol", rolSchema, rol) as Promise<
    ApiResponse<RolResponse>
  >;

export const deleteRol = async (
  id: number,
): Promise<ApiResponse<RolResponse>> =>
  zodApi.delete<Rol>(`Rol/${id}`, rolSchema) as Promise<
    ApiResponse<RolResponse>
  >;
