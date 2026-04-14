import {
  Button,
  FieldError,
  Form,
  Input,
  Label,
  TextField,
} from "@heroui/react";
import { useCallback, type ChangeEvent } from "react";
import { Link } from "react-router";
import { nameRoutes } from "../../configs/constants";
import { useForm } from "../../hooks/useForm";
import type { ApiResponse } from "../../types/ApiResponse";
import { validateLogin } from "../../validations/loginValidation";
import { Response } from "../messages/Response";

export interface LoginFormData {
  userName: string;
  password: string;
}

export interface LoginFormProps {
  readonly initialForm: LoginFormData;
  readonly onSubmit: (form: LoginFormData) => Promise<ApiResponse<unknown>>;
}

export function LoginForm({ initialForm, onSubmit }: LoginFormProps) {
  const { form, errors, handleChange, handleSubmit, success, message } =
    useForm<LoginFormData, unknown>(initialForm, validateLogin, onSubmit);

  const handleTextChange = useCallback(
    (name: string) => (val: string) => {
      handleChange({
        target: { name, value: val },
      } as unknown as ChangeEvent<HTMLInputElement>);
    },
    [handleChange],
  );

  return (
    <>
      {success != null && <Response message={message} type={success} />}
      <Form
        className="flex flex-col w-full"
        validationErrors={errors}
        onSubmit={handleSubmit}
      >
        <TextField
          isRequired
          className="w-full flex flex-col gap-1"
          isInvalid={!!errors?.userName}
          name="userName"
          onChange={handleTextChange("userName")}
        >
          <Label className="font-bold">Nombre de usuario</Label>
          <Input
            className="w-full px-3 py-2 border rounded-md"
            type="text"
            value={form.userName || ""}
            variant="primary"
          />
          {errors?.userName ? (
            <FieldError>{errors.userName as string}</FieldError>
          ) : null}
        </TextField>

        <TextField
          isRequired
          className="w-full flex flex-col gap-1"
          isInvalid={!!errors?.password}
          name="password"
          onChange={handleTextChange("password")}
        >
          <Label className="font-bold">Contraseña</Label>
          <Input
            className="w-full px-3 py-2 border rounded-md"
            type="password"
            value={form.password || ""}
            variant="primary"
          />
          {errors?.password ? (
            <FieldError>{errors.password as string}</FieldError>
          ) : null}
        </TextField>

        <Button
          className="py-4 mt-4 font-bold w-full"
          size="lg"
          type="submit"
          variant="primary"
        >
          Iniciar Sesión
        </Button>
      </Form>
      <div className="flex flex-col items-center mt-4 gap-2">
        <Link
          className="font-bold underline text-cyan-500"
          to={nameRoutes.changePassword}
        >
          Olvido su contraseña?
        </Link>
        <Link
          className="font-bold underline text-cyan-500"
          to={nameRoutes.register}
        >
          No tienes cuenta? Registrate
        </Link>
      </div>
    </>
  );
}
