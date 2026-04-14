import { Card } from "@heroui/react";

import { LoginForm, type LoginFormData } from "../../components/form/LoginForm";

import { useCallback } from "react";
import { Images } from "../../assets/images/images";
import { useAuth } from "../../hooks/useAuth";
import ProtectedLogin from "../../routes/middlewares/ProtectedLogin";
import { authenticateUser } from "../../services/authService";

const initialForm: LoginFormData = {
  userName: "",
  password: "",
};

export function Component() {
  const { signIn } = useAuth();

  const petition = useCallback(
    async (form: LoginFormData) => {
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
    },
    [signIn],
  );

  return (
    <ProtectedLogin>
      <section className="flex flex-col md:flex-row justify-center items-center w-screen h-screen">
        <div className="flex items-center px-6 md:mx-auto w-full md:max-w-md lg:max-w-lg xl:max-w-xl">
          <Card className="w-full shadow-[0px_20px_20px_10px_#A0AEC0]">
            <div className="p-10 flex flex-col w-full">
              <div className="flex justify-center">
                <img
                  alt="Esi Logo"
                  className=""
                  src={Images.logo}
                  width={240}
                />
              </div>
              <LoginForm initialForm={initialForm} onSubmit={petition} />
            </div>
          </Card>
        </div>
      </section>
    </ProtectedLogin>
  );
}

Component.displayName = "LoginPage";
