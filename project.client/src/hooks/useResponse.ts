import { useState } from "react";
import type { ApiResponse } from "../types/ApiResponse";
import type { ValidationFailure } from "../types/ValidationFailure";
import { toCamelCase } from "../utils/converted";
import type { ErrorObject } from "./useForm";

export const useResponse = <T, U>() => {
  const [dataResult, setDataResult] = useState<T>();
  const [fieldErrors, setFieldErrors] = useState<ErrorObject>();
  const [apiMessage, setApiMessage] = useState<string>("");
  const [success, setSuccess] = useState<boolean | null>(null);

  const mapValidationFailuresToFieldErrors = (errors: ValidationFailure[]) => {
    const errorsConverted: ErrorObject = {};

    errors.forEach((error) => {
      errorsConverted[toCamelCase(error.propertyName)] = error.errorMessage;
    });

    if (Object.keys(errorsConverted).length !== 0) {
      setErrorsResponse(errorsConverted);
    }
  };

  const setErrorsResponse = (errors: ErrorObject) => {
    setFieldErrors(errors);
  };

  const handleApiResponse = ({
    data,
    success,
    message,
  }: ApiResponse<T | U>) => {
    if (success) {
      setDataResult(data as T);
    } else {
      mapValidationFailuresToFieldErrors(
        (data as unknown as ValidationFailure[]) ?? [],
      );
    }
    setSuccess(success);
    setApiMessage(message!);
  };

  return {
    dataResult,
    fieldErrors,
    success,
    handleApiResponse,
    setErrorsResponse,
    apiMessage,
  };
};
