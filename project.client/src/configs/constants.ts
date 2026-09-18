import type { InitialAuth } from "../types/InitialAuth";
import type { UserRequest } from "../types/UserRequest";

// F11: API base URL comes from `import.meta.env.VITE_API_URL` (see
// `configs/axios/interceptors.ts`). Never hard-code it here.

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
  user: "/user",
  userCreate: "/user/create",
  userUpdate: "/user/update",
  rol: "/rol",
  rolCreate: "/rol/create",
  rolUpdate: "/rol/update",
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

export const initialUser: UserRequest = {
  id: null,
  rolId: null,
  email: null,
  name: null,
  userName: null,
  password: null,
  identificationDocument: null,
  number: null,
  state: null,
  createdBy: null,
  updatedBy: null,
};
