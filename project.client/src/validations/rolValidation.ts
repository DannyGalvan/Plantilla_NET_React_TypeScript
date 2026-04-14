import { z } from "zod";
import { invalid_type_error } from "../configs/constants";
import type { ErrorObject } from "../hooks/useForm";
import { handleOneLevelZodError } from "../utils/converted";

export const rolSchema = z.object({
  id: z.number().optional(),
  name: z
    .string()
    .min(1, "El nombre es obligatorio")
    .max(100, "El nombre no debe exceder los 100 caracteres"),
  description: z
    .string()
    .min(1, "La descripción es obligatoria")
    .max(250, "La descripción no debe exceder los 250 caracteres"),
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

export type RolFormValues = z.infer<typeof rolSchema>;

export const validateRol = (data: unknown) => {
  const result = rolSchema.safeParse(data);

  if (!result.success) {
    let errors: ErrorObject = {};
    errors = handleOneLevelZodError(result.error);
    return errors;
  }

  return {};
};
