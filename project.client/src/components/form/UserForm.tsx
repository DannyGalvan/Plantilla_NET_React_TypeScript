import {
  Button,
  FieldError,
  Form,
  Input,
  Label,
  TextField,
} from "@heroui/react";
import { useCallback, type ChangeEvent } from "react";
import { useNavigate } from "react-router";
import type { SingleValue } from "react-select";
import { useForm } from "../../hooks/useForm";
import { getRoles } from "../../services/rolService";
import type { ApiResponse } from "../../types/ApiResponse";
import type { RolResponse } from "../../types/RolResponse";
import type { UserRequest } from "../../types/UserRequest";
import type { ValidationFailure } from "../../types/ValidationFailure";
import { validateUser } from "../../validations/userValidation";
import { Response } from "../messages/Response";
import { CatalogueSelect } from "../select/CatalogueSelect";
import { OptionsSelect, type OptionValue } from "../select/OptionsSelect";

interface CreateUserFormProps {
  readonly type: "create";
  readonly initialForm: UserRequest;
  readonly onSubmit: (
    form: UserRequest,
  ) => Promise<ApiResponse<unknown | ValidationFailure[]>>;
}

interface EditUserFormProps {
  readonly type: "edit";
  readonly initialForm: UserRequest;
  readonly onSubmit: (
    form: UserRequest,
  ) => Promise<ApiResponse<unknown | ValidationFailure[]>>;
}

type UserFormProps = CreateUserFormProps | EditUserFormProps;

export function UserForm({ type, initialForm, onSubmit }: UserFormProps) {
  const isEditing = type === "edit";
  const navigate = useNavigate();

  const { form, errors, handleChange, handleSubmit, success, message } =
    useForm<UserRequest, unknown>(initialForm, validateUser, onSubmit, true);

  const handleTextChange = useCallback(
    (name: string) => (val: string) => {
      handleChange({
        target: { name, value: val },
      } as unknown as ChangeEvent<HTMLInputElement>);
    },
    [handleChange],
  );

  const handleSelectChange = useCallback(
    (name: string) => (opt: OptionValue) => {
      const option = opt as SingleValue<{ label: string; value: string }>;
      handleChange({
        target: { name, value: option?.value || "" },
      } as React.ChangeEvent<HTMLInputElement>);
    },
    [handleChange],
  );

  const selectorRol = useCallback(
    (item: RolResponse) => ({
      label: item.description,
      value: String(item.id),
    }),
    [],
  );

  const handleCancel = useCallback(() => {
    navigate("/user");
  }, [navigate]);

  return (
    <div className="max-w-2xl mx-auto p-6">
      <h1 className="text-2xl font-bold text-center mb-6">
        {isEditing ? "Editar Usuario" : "Crear Usuario"}
      </h1>

      {success != null && <Response message={message} type={success} />}

      <Form
        className="flex flex-col gap-4"
        validationErrors={errors}
        onSubmit={handleSubmit}
      >
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <TextField
            isRequired
            className="w-full flex flex-col gap-1"
            isInvalid={!!errors?.name}
            name="name"
            onChange={handleTextChange("name")}
          >
            <Label className="font-bold">Nombre Completo</Label>
            <Input
              className="w-full px-3 py-2 border rounded-md"
              type="text"
              value={form.name || ""}
            />

            {errors?.name ? (
              <FieldError>{errors.name as string}</FieldError>
            ) : null}
          </TextField>

          <TextField
            isRequired
            className="w-full flex flex-col gap-1"
            isInvalid={!!errors?.email}
            name="email"
            onChange={handleTextChange("email")}
          >
            <Label className="font-bold">Correo Electrónico</Label>
            <Input
              className="w-full px-3 py-2 border rounded-md"
              type="email"
              value={form.email || ""}
            />

            {errors?.email ? (
              <FieldError>{errors.email as string}</FieldError>
            ) : null}
          </TextField>

          <TextField
            isRequired
            className="w-full flex flex-col gap-1"
            isInvalid={!!errors?.userName}
            name="userName"
            onChange={handleTextChange("userName")}
          >
            <Label className="font-bold">Nombre de Usuario</Label>
            <Input
              className="w-full px-3 py-2 border rounded-md"
              type="text"
              value={form.userName || ""}
            />

            {errors?.userName ? (
              <FieldError>{errors.userName as string}</FieldError>
            ) : null}
          </TextField>

          <TextField
            className="w-full flex flex-col gap-1"
            isInvalid={!!errors?.password}
            isRequired={!isEditing}
            name="password"
            onChange={handleTextChange("password")}
          >
            <Label className="font-bold">
              {isEditing ? "Nueva Contraseña (opcional)" : "Contraseña"}
            </Label>
            <Input
              className="w-full px-3 py-2 border rounded-md"
              type="password"
              value={form.password || ""}
            />

            {errors?.password ? (
              <FieldError>{errors.password as string}</FieldError>
            ) : null}
          </TextField>

          <TextField
            className="w-full flex flex-col gap-1"
            isInvalid={!!errors?.identificationDocument}
            name="identificationDocument"
            onChange={handleTextChange("identificationDocument")}
          >
            <Label className="font-bold">Documento de Identificación</Label>
            <Input
              className="w-full px-3 py-2 border rounded-md"
              type="text"
              value={form.identificationDocument || ""}
            />

            {errors?.identificationDocument ? (
              <FieldError>{errors.identificationDocument as string}</FieldError>
            ) : null}
          </TextField>

          <TextField
            className="w-full flex flex-col gap-1"
            isInvalid={!!errors?.number}
            name="number"
            onChange={handleTextChange("number")}
          >
            <Label className="font-bold">Número de Teléfono</Label>
            <Input
              className="w-full px-3 py-2 border rounded-md"
              type="text"
              value={form.number || ""}
            />

            {errors?.number ? (
              <FieldError>{errors.number as string}</FieldError>
            ) : null}
          </TextField>

          <CatalogueSelect
            isRequired
            defaultValue={
              type === "edit"
                ? {
                    label: initialForm.rol?.name ?? "",
                    value: initialForm.rolId?.toString() ?? "",
                  }
                : null
            }
            deps="State:eq:1"
            errorMessage={errors?.rolId as string}
            fieldSearch="Name"
            isInvalid={!!errors?.rolId}
            label="Rol"
            name="rolId"
            placeholder="Seleccione un rol"
            queryFn={getRoles}
            selectorFn={selectorRol}
            onChange={handleSelectChange("rolId")}
          />

          <OptionsSelect
            isRequired
            defaultValue={
              form.state !== null && form.state !== undefined
                ? {
                    label: form.state === 1 ? "Activo" : "Inactivo",
                    value: form.state.toString(),
                  }
                : { label: "Activo", value: "1" }
            }
            errorMessage={errors?.state as string}
            isInvalid={!!errors?.state}
            label="Estado"
            name="state"
            options={[
              { label: "Activo", value: "1" },
              { label: "Inactivo", value: "0" },
            ]}
            placeholder="Seleccione un estado"
            onChange={handleSelectChange("state")}
          />
        </div>

        <div className="flex gap-4 justify-end mt-4">
          <Button
            className="font-bold"
            size="lg"
            type="button"
            variant="secondary"
            onClick={handleCancel}
          >
            Cancelar
          </Button>
          <Button
            className="font-bold"
            size="lg"
            type="submit"
            variant="primary"
          >
            {isEditing ? "Actualizar" : "Crear"}
          </Button>
        </div>
      </Form>
    </div>
  );
}
