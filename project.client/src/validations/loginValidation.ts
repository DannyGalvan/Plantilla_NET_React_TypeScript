import { z } from "zod";
import type { ErrorObject } from "../hooks/useForm";
import { handleOneLevelZodError } from "../utils/converted";

export const loginSchema = z.object({
  userName: z.string().min(1, "El campo nombre de usuario es requerido"),
  password: z
    .string()
    .min(1, "El campo password es requerido")
    .min(6, "El password debe tener al menos 6 caracteres"),
});

export type LoginValidation = z.infer<typeof loginSchema>;

export const validateLogin = (data: unknown) => {
  const result = loginSchema.safeParse(data);

  if (!result.success) {
    let errors: ErrorObject = {};
    errors = handleOneLevelZodError(result.error);
    return errors;
  }

  return {};
};
