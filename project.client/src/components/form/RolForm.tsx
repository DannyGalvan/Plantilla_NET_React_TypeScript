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
import { useForm } from "../../hooks/useForm";
import type { ApiResponse } from "../../types/ApiResponse";
import type { RolRequest } from "../../types/RolRequest";
import type { ValidationFailure } from "../../types/ValidationFailure";
import { validateRol } from "../../validations/rolValidation";
import { Response } from "../messages/Response";
import { OptionsSelect, type OptionValue } from "../select/OptionsSelect";

interface CreateRolFormProps {
  readonly type: "create";
  readonly initialForm: RolRequest;
  readonly onSubmit: (
    form: RolRequest,
  ) => Promise<ApiResponse<unknown | ValidationFailure[]>>;
}

interface EditRolFormProps {
  readonly type: "edit";
  readonly initialForm: RolRequest;
  readonly onSubmit: (
    form: RolRequest,
  ) => Promise<ApiResponse<unknown | ValidationFailure[]>>;
}

type RolFormProps = CreateRolFormProps | EditRolFormProps;

export function RolForm({ type, initialForm, onSubmit }: RolFormProps) {
  const isEditing = type === "edit";
  const navigate = useNavigate();

  const { form, errors, handleChange, handleSubmit, success, message } =
    useForm<RolRequest, unknown>(initialForm, validateRol, onSubmit, true);

  const handleTextChange = useCallback(
    (name: string) => (val: string) => {
      handleChange({
        target: { name, value: val },
      } as unknown as ChangeEvent<HTMLInputElement>);
    },
    [handleChange],
  );

  const handleStateChange = useCallback(
    (newValue: OptionValue) => {
      const value =
        newValue && !Array.isArray(newValue) && "value" in newValue
          ? Number((newValue as { value: string }).value)
          : null;

      handleChange({
        target: { name: "state", value },
      } as unknown as ChangeEvent<HTMLInputElement>);
    },
    [handleChange],
  );

  const handleCancel = useCallback(() => {
    navigate("/rol");
  }, [navigate]);

  const stateOptions = [
    { label: "Activo", value: "1" },
    { label: "Inactivo", value: "0" },
  ];

  const currentState =
    form.state !== null && form.state !== undefined
      ? {
          label: form.state === 1 ? "Activo" : "Inactivo",
          value: form.state.toString(),
        }
      : { label: "Activo", value: "1" };

  return (
    <div className="max-w-2xl mx-auto p-6">
      <h1 className="text-2xl font-bold text-center mb-6">
        {isEditing ? "Editar Rol" : "Crear Rol"}
      </h1>

      {success != null && <Response message={message} type={success} />}

      <Form
        className="flex flex-col gap-4"
        validationErrors={errors}
        onSubmit={handleSubmit}
      >
        <div className="grid grid-cols-1 gap-4">
          <TextField
            isRequired
            className="w-full flex flex-col gap-1"
            isInvalid={!!errors?.name}
            name="name"
            onChange={handleTextChange("name")}
          >
            <Label className="font-bold">Nombre del Rol</Label>
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
            isInvalid={!!errors?.description}
            name="description"
            onChange={handleTextChange("description")}
          >
            <Label className="font-bold">Descripción</Label>
            <Input
              className="w-full px-3 py-2 border rounded-md"
              type="text"
              value={form.description || ""}
            />

            {errors?.description ? (
              <FieldError>{errors.description as string}</FieldError>
            ) : null}
          </TextField>

          <OptionsSelect
            isRequired
            defaultValue={currentState}
            errorMessage={errors?.state as string}
            isInvalid={!!errors?.state}
            label="Estado"
            name="state"
            options={stateOptions}
            placeholder="Seleccione un estado"
            onChange={handleStateChange}
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
