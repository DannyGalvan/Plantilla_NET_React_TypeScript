import { z } from "zod";

import type { ChangePasswordForm } from "../pages/auth/ChangePasswordPage";
import type { ApiResponse } from "../types/ApiResponse";
import type { LoginRequest, LoginResponse } from "../types/LoginRequest";
import { authWithRefreshResponseSchema } from "../types/schemas";

import { zodApi } from "./zodApi";

const authResponseSchema = authWithRefreshResponseSchema;
const changePasswordResponseSchema = z.string();

type AuthResponse = z.infer<typeof authResponseSchema>;

export const authenticateUser = async (
  login: LoginRequest,
): Promise<ApiResponse<LoginResponse>> =>
  zodApi.post<AuthResponse, LoginRequest>(
    "/auth",
    authResponseSchema,
    login,
  ) as Promise<ApiResponse<LoginResponse>>;

export const changePassword = async (
  credentials: ChangePasswordForm,
): Promise<ApiResponse<string>> =>
  zodApi.post<string, ChangePasswordForm>(
    "/Auth/ResetPassword",
    changePasswordResponseSchema,
    credentials,
  );
