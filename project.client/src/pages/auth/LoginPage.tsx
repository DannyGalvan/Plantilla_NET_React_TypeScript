import { Button, Card, Form, TextField } from "@heroui/react";
import { useCallback, type ChangeEvent } from "react";
import { Link } from "react-router";

import { Response } from "../../components/messages/Response";

import { Images } from "../../assets/images/images";
import { nameRoutes } from "../../configs/constants";
import { useAuth } from "../../hooks/useAuth";
import { useForm } from "../../hooks/useForm";
import ProtectedLogin from "../../routes/middlewares/ProtectedLogin";
import { authenticateUser } from "../../services/authService";
import { validateLogin } from "../../validations/loginValidation";

export interface LoginForm {
  userName: string;
  password: string;
}

const initialForm = {
  userName: "",
  password: "",
};

export function Component() {
  const { signIn } = useAuth();

  const petition = async (form: LoginForm) => {
    const response = await authenticateUser(form);

    if (!response.success) {
      return response;
    }

    const authentication = response.data;

    signIn({
      email: authentication.email,
      token: authentication.token,
      userName: authentication.userName,
      name: authentication.name,
      operations: authentication.operations,
      redirect: false,
      isLoggedIn: true,
      userId: authentication.userId,
    });
    return response;
  };

  const { form, errors, handleChange, handleSubmit, success, message } =
    useForm<LoginForm, unknown>(initialForm, validateLogin, petition);

  const handleUserNameChange = useCallback(
    (val: string) => {
      handleChange({
        target: { name: "userName", value: val },
      } as unknown as ChangeEvent<HTMLInputElement>);
    },
    [handleChange],
  );

  const handlePasswordChange = useCallback(
    (val: string) => {
      handleChange({
        target: { name: "password", value: val },
      } as unknown as ChangeEvent<HTMLInputElement>);
    },
    [handleChange],
  );

  return (
    <ProtectedLogin>
      <section className="flex flex-col md:flex-row justify-center items-center w-screen h-screen">
        <div className="flex items-center px-6 md:mx-auto w-full md:max-w-md lg:max-w-lg xl:max-w-xl">
          <Card className="w-full shadow-[0px_20px_20px_10px_#A0AEC0]">
            <div className="p-10 flex flex-col w-full">
              {success != null && <Response message={message} type={success} />}
              <div className="flex justify-center">
                <img
                  alt="Esi Logo"
                  className=""
                  src={Images.logo}
                  width={240}
                />
              </div>
              <Form
                className="flex flex-col w-full"
                validationErrors={errors}
                onSubmit={handleSubmit}
              >
                <label className="font-bold">Nombre de usuario</label>
                <TextField
                  isRequired
                  className="py-4"
                  id="email"
                  name="userName"
                  type="text"
                  value={form.userName}
                  onChange={handleUserNameChange}
                />
                <label className="font-bold">Contraseña</label>
                <TextField
                  isRequired
                  className="py-4"
                  id="password"
                  name="password"
                  type="password"
                  value={form.password}
                  onChange={handlePasswordChange}
                />
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
                  className="font-bold underline  text-cyan-500"
                  to={nameRoutes.changePassword}
                >
                  Olvido su contraseña?
                </Link>
                <Link
                  className="font-bold underline  text-cyan-500"
                  to={nameRoutes.register}
                >
                  No tienes cuenta? Registrate
                </Link>
              </div>
            </div>
          </Card>
        </div>
      </section>
    </ProtectedLogin>
  );
}

Component.displayName = "LoginPage";
