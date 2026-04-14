import type { InitialAuth } from "../types/InitialAuth";

export const URL_BASE = "";
export const API_URL = `${URL_BASE}/api/v1/`;

export const invalid_type_error = "El tipo provisto es invalido";
export const required_error = "El campo es requerido";

export const nameRoutes = {
  login: "/auth",
  register: "/register",
  changePassword: "/change-password",
  settings: "/change-password",
  root: "/",
  notFound: "*",
  forbidden: "/forbidden",
  unauthorized: "/unauthorized",
  error: "/error",
  create: "create",
};

export const authInitialState: InitialAuth = {
  isLoggedIn: false,
  redirect: false,
  email: "",
  token: "",
  userName: "",
  name: "",
  userId: 0,
  operations: [],
};

export const PAGINATION_OPTIONS = {
  rowsPerPageText: "Elementos Por página",
  rangeSeparatorText: "de",
  selectAllRowsItem: false,
  selectAllRowsItemText: "Todos",
};

export const SELECTED_MESSAGE = {
  singular: "Elemento",
  plural: "Elementos",
  message: "Seleccionado(s)",
};
