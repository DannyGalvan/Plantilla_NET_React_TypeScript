import { z } from "zod";
import type { ErrorObject } from "../hooks/useForm";
import { handleOneLevelZodError } from "../utils/converted";

export const changePasswordSchema = z
  .object({
    idUser: z.number().default(0),
    password: z
      .string()
      .min(1, "La contraseña es requerida")
      .min(6, "La contraseña debe tener al menos 6 caracteres"),
    confirmPassword: z
      .string()
      .min(1, "La confirmación de contraseña es requerida"),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: "Las contraseñas no coinciden",
    path: ["confirmPassword"],
  });

export type ChangePasswordValidation = z.infer<typeof changePasswordSchema>;

export const validateChangePassword = (data: unknown) => {
  const result = changePasswordSchema.safeParse(data);

  if (!result.success) {
    let errors: ErrorObject = {};
    errors = handleOneLevelZodError(result.error);
    return errors;
  }

  return {};
};
