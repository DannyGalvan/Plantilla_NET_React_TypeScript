import { useEffect, useState } from "react";
import { useNavigate } from "react-router";

import { Col } from "../../components/grid/Col";
import { Row } from "../../components/grid/Row";
import { nameRoutes } from "../../configs/constants";
import { useAuth } from "../../hooks/useAuth";

interface NotFoundProps {
  readonly Message: string;
  readonly Number: string;
}

export function NotFound({ Message, Number }: NotFoundProps) {
  const navigate = useNavigate();
  const { isLoggedIn, logout } = useAuth();

  const [text, setText] = useState("Regresar");

  useEffect(() => {
    if (Number === "404" || Number === "403") {
      setText("Regresar");
    } else if (Number === "500" || Number === "401") {
      logout();
      navigate(nameRoutes.login);
    }
  }, [Number]);

  const handleClick = () => {
    if (Number === "404") {
      if (isLoggedIn) {
        navigate(-1);
      } else {
        navigate(nameRoutes.login);
      }
    } else if (Number === "403") {
      navigate(-3);
    }
  };

  return (
    <div className="container mx-auto my-auto">
      <Row className="min-h-[80vh] items-center justify-center">
        <Col className="text-center" xs={12}>
          <span className="block text-8xl font-bold">{Number}</span>
          <div className="mb-4 text-3xl italic">{Message}</div>
          <span
            className="cursor-pointer font-bold text-sky-700 hover:text-sky-500"
            onClick={handleClick}
          >
            {text}
          </span>
        </Col>
      </Row>
    </div>
  );
}
