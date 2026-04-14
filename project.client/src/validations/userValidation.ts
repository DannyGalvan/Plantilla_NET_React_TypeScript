import { z } from "zod";
import { invalid_type_error } from "../configs/constants";
import type { ErrorObject } from "../hooks/useForm";
import { handleOneLevelZodError } from "../utils/converted";

export const userSchema = z.object({
  id: z.number().nullable().optional(),
  rolId: z.union([
    z
      .string({
        error: invalid_type_error,
      })
      .min(1, "El Rol es requerido"),
    z.number().min(1, "El Rol es requerido"),
  ]),
  email: z
    .string()
    .min(1, "El Email es requerido")
    .email("El Email no es válido")
    .max(100, "El Email no puede exceder 100 caracteres"),
  name: z
    .string()
    .min(1, "El Nombre es requerido")
    .max(150, "El Nombre no puede exceder 150 caracteres"),
  userName: z
    .string()
    .min(4, "El Nombre de Usuario debe tener al menos 4 caracteres")
    .max(50, "El Nombre de Usuario no puede exceder 50 caracteres"),
  password: z
    .string()
    .min(6, "La Contraseña debe tener al menos 6 caracteres")
    .max(100, "La Contraseña no puede exceder 100 caracteres")
    .optional()
    .or(z.literal("")),
  identificationDocument: z
    .string()
    .max(50, "El Documento de Identificación no puede exceder 50 caracteres")
    .optional()
    .or(z.literal("")),
  number: z
    .string()
    .max(20, "El Número no puede exceder 20 caracteres")
    .optional()
    .or(z.literal("")),
  state: z.union([
    z
      .string({
        error: invalid_type_error,
      })
      .min(0)
      .max(1, "El Estado debe ser 0 (Inactivo) o 1 (Activo)"),
    z.number().min(0).max(1, "El Estado debe ser 0 (Inactivo) o 1 (Activo)"),
  ]),
});

export type UserValidation = z.infer<typeof userSchema>;

export const validateUser = (data: unknown) => {
  const result = userSchema.safeParse(data);

  if (!result.success) {
    let errors: ErrorObject = {};
    errors = handleOneLevelZodError(result.error);
    return errors;
  }

  return {};
};
