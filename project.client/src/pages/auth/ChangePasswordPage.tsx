import { Button, Label, TextField } from "@heroui/react";
import type { ChangeEvent } from "react";
import { useCallback } from "react";
import { Col } from "../../components/grid/Col";
import { Response } from "../../components/messages/Response";
import { useAuth } from "../../hooks/useAuth";
import { useForm } from "../../hooks/useForm";
import ProtectedPublic from "../../routes/middlewares/ProtectedPublic";
import { changePassword } from "../../services/authService";
import { validateChangePassword } from "../../validations/changePasswordValidation";

export interface ChangePasswordForm {
  idUser: number;
  password: string;
  confirmPassword: string;
}

const initialForm: ChangePasswordForm = {
  idUser: 0,
  password: "",
  confirmPassword: "",
};

export function Component() {
  const { userId, logout } = useAuth();

  const sendForm = async (form: ChangePasswordForm) => {
    form.idUser = userId;
    const response = await changePassword(form);
    if (response.success) {
      logout();
    }
    return response;
  };

  const { form, handleChange, handleSubmit, success, message } = useForm(
    initialForm,
    validateChangePassword,
    sendForm,
  );

  const handlePasswordChange = useCallback(
    (val: string) => {
      handleChange({
        target: { name: "password", value: val },
      } as unknown as ChangeEvent<HTMLInputElement>);
    },
    [handleChange],
  );

  const handleConfirmPasswordChange = useCallback(
    (val: string) => {
      handleChange({
        target: { name: "confirmPassword", value: val },
      } as unknown as ChangeEvent<HTMLInputElement>);
    },
    [handleChange],
  );

  return (
    <ProtectedPublic>
      <div className="page-view container flex flex-col flex-wrap items-center justify-center">
        <Col lg={8} md={10} sm={12} xl={6}>
          {success != null && <Response message={message} type={success} />}
          <h1 className="text-center text-4xl font-bold">Cambiar contraseña</h1>
        </Col>
        <form
          className="flex flex-row flex-wrap justify-center col-xl-6 col-lg-8 col-md-10 col-12"
          onSubmit={handleSubmit}
        >
          <Col md={12}>
            <Label className="font-bold">Contraseña</Label>
            <TextField
              isRequired
              className="py-4"
              name="password"
              type="password"
              value={form.password}
              onChange={handlePasswordChange}
            />
          </Col>
          <Col md={12}>
            <Label className="font-bold">Confirmacion de contraseña</Label>
            <TextField
              isRequired
              className="py-4"
              name="confirmPassword"
              type="password"
              value={form.confirmPassword}
              onChange={handleConfirmPasswordChange}
            />
          </Col>
          <Col className="mt-5" md={12}>
            <Button
              className="py-4 mt-4 font-bold w-full"
              size="lg"
              type="submit"
              variant="primary"
            >
              Cambiar contraseña
            </Button>
          </Col>
        </form>
      </div>
    </ProtectedPublic>
  );
}

Component.displayName = "ChangePasswordPage";
