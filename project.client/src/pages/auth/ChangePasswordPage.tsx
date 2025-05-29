import { Button } from "@heroui/button";
import { Input } from "@heroui/input";
import { Col } from "../../components/grid/Col";
import { Icon } from "../../components/icons/Icon";
import { Response } from "../../components/messages/Response";
import { useAuth } from "../../hooks/useAuth";
import { useForm, type ErrorObject } from "../../hooks/useForm";
import { useToggle } from "../../hooks/useToggle";
import ProtectedPublic from "../../routes/middlewares/ProtectedPublic";
import { changePassword } from "../../services/authService";

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

const validateForm = (form: ChangePasswordForm) => {
  const errors: ErrorObject = {};

  if (!form.password.trim()) {
    errors.password = "La contraseña es requerida";
  }
  if (!form.confirmPassword.trim()) {
    errors.confirmPassword = "La confirmación de contraseña es requerida";
  }
  if (form.password.trim() !== form.confirmPassword.trim()) {
    errors.confirmPassword = "Las contraseñas no coinciden";
  }

  return errors;
};

export function Component() {
  const { userId, logout } = useAuth();
  const { open: password, toggle: togglePassword } = useToggle();
  const { open: confirm, toggle: toggleConfirm } = useToggle();

  const sendForm = async (form: ChangePasswordForm) => {
    form.idUser = userId;
    const response = await changePassword(form);
    if (response.success) {
      logout();
    }
    return response;
  };

  const {
    form,
    errors,
    handleChange,
    handleSubmit,
    success,
    loading,
    message,
  } = useForm(initialForm, validateForm, sendForm);

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
            <Input
              isRequired
              className="py-4"
              endContent={
                <button
                  className="focus:outline-none"
                  type="button"
                  onClick={togglePassword}
                >
                  {password ? (
                    <Icon name="bi bi-eye-slash-fill text-2xl text-dark pointer-events-none" />
                  ) : (
                    <Icon name="bi bi-eye-fill text-2xl text-dark pointer-events-none" />
                  )}
                </button>
              }
              errorMessage={errors?.password}
              isInvalid={!!errors?.password}
              label="Contraseña"
              name="password"
              size="lg"
              type={password ? "text" : "password"}
              value={form.password}
              variant="bordered"
              onChange={handleChange}
            />
          </Col>
          <Col md={12}>
            <Input
              isRequired
              className="py-4"
              endContent={
                <button
                  className="focus:outline-none"
                  type="button"
                  onClick={toggleConfirm}
                >
                  {confirm ? (
                    <Icon name="bi bi-eye-slash-fill text-2xl text-dark pointer-events-none" />
                  ) : (
                    <Icon name="bi bi-eye-fill text-2xl text-dark pointer-events-none" />
                  )}
                </button>
              }
              errorMessage={errors?.confirmPassword}
              isInvalid={!!errors?.confirmPassword}
              label="Confirmacion de contraseña"
              name="confirmPassword"
              size="lg"
              type={confirm ? "text" : "password"}
              value={form.confirmPassword}
              variant="bordered"
              onChange={handleChange}
            />
          </Col>
          <Col className="mt-5" md={12}>
            <Button
              fullWidth
              className="py-4 mt-4 font-bold"
              color="primary"
              isLoading={loading}
              radius="md"
              size="lg"
              type="submit"
              variant="shadow"
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
