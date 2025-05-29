import { api } from "../configs/axios/interceptors";
import type { ChangePasswordForm } from "../pages/auth/ChangePasswordPage";
import type { ApiResponse } from "../types/ApiResponse";
import type { LoginRequest, LoginResponse } from "../types/LoginRequest";
import type { ValidationFailure } from "../types/ValidationFailure";

export const authenticateUser = async (login: LoginRequest) => {
  const response = await api.post<
    unknown,
    ApiResponse<LoginResponse>,
    LoginRequest
  >("/auth", login);

  return response;
};

export const changePassword = async (credentials: ChangePasswordForm) => {
  return await api.post<
    unknown,
    ApiResponse<string | ValidationFailure[]>,
    ChangePasswordForm
  >("/Auth/ResetPassword", credentials);
};
