import { useEffect, useState } from "react";
import { useNavigate } from "react-router";

import { Col } from "../../components/grid/Col";
import { Row } from "../../components/grid/Row";
import { nameRoutes } from "../../configs/constants";
import { useAuth } from "../../hooks/useAuth";
import ProtectedError from "../../routes/middlewares/ProtectedError";
import { useErrorsStore } from "../../stores/useErrorsStore";

export function Component() {
  const navigate = useNavigate();
  const { error, resetError } = useErrorsStore();
  const { logout, isLoggedIn } = useAuth();

  const [text, setText] = useState("Regresar");

  useEffect(() => {
    if (error?.statusCode === "404" || error?.statusCode === "403") {
      setText("Regresar");
    } else if (error?.statusCode === "500" || error?.statusCode === "401") {
      resetError();
      logout();
      navigate(nameRoutes.login);
    }
  }, [error]);

  const handleClick = () => {
    resetError();
    if (error?.statusCode === "404") {
      if (isLoggedIn) {
        navigate(-1);
      } else {
        navigate(nameRoutes.login);
      }
    } else if (error?.statusCode === "403") {
      navigate(-3);
    } else {
      navigate(-1);
    }
  };

  return (
    <ProtectedError>
      <div className="container mx-auto">
        <Row className="min-h-[80vh] items-center justify-center">
          <Col className="text-center" xs={12}>
            <span className="block text-8xl font-bold">
              {error?.statusCode ?? error?.name}
            </span>
            <div className="mb-4 text-3xl italic">{error?.message}</div>
            <span
              className="cursor-pointer font-bold text-sky-700 hover:text-sky-500"
              onClick={handleClick}
            >
              {text}
            </span>
          </Col>
        </Row>
      </div>
    </ProtectedError>
  );
}

Component.displayName = "ErrorPage";
