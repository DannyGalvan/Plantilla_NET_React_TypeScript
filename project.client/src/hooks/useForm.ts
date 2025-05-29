import type { ChangeEvent, FormEvent } from "react";
import { useEffect, useState } from "react";

import { useErrorsStore } from "../stores/useErrorsStore";
import type { ApiResponse } from "../types/ApiResponse";
import { ApiError } from "../types/errors";
import type { ValidationFailure } from "../types/ValidationFailure";
import { useResponse } from "./useResponse";

export interface ErrorObject {
  [key: string]: string | string[];
}

export const useForm = <T, U>(
  initialForm: T,
  validateForm: (form: T) => ErrorObject,
  peticion: (form: T) => Promise<ApiResponse<U | ValidationFailure[]>>,
  reboot?: boolean,
) => {
  const { setError } = useErrorsStore();
  const [form, setForm] = useState<T>(initialForm);
  const [loading, setLoading] = useState<boolean>(false);
  const {
    success,
    handleApiResponse,
    dataResult,
    fieldErrors,
    apiMessage,
    setErrorsResponse,
  } = useResponse<U, ValidationFailure[]>();

  useEffect(() => {
    setForm(initialForm);
  }, [initialForm]);

  const handleChange = (e: ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;

    const newForm = {
      ...form,
      [name]: value,
    };

    setForm(newForm);
    setErrorsResponse(validateForm(newForm));
  };

  const handleChangeFile = (e: ChangeEvent<HTMLInputElement>) => {
    const { name, files } = e.target;

    if (files != null) {
      const newForm = {
        ...form,
        [name]: files[0],
      };

      setForm(newForm);

      setErrorsResponse(validateForm(newForm));
    } else {
      const newForm = {
        ...form,
        [name]: null,
      };

      setForm(newForm);

      setErrorsResponse(validateForm(newForm));
    }
  };

  const handleBlur = (e: ChangeEvent<HTMLInputElement>) => {
    handleChange(e);
  };

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    e.stopPropagation();

    handleApiResponse({
      success: null,
      data: null,
      message: "Error en la validación del formulario",
      totalResults: 0,
    });

    const valErr = validateForm(form);
    setErrorsResponse(valErr);
    setLoading(true);

    if (Object.keys(valErr).length === 0) {
      try {
        const response = await peticion(form);

        if (response.success) {
          if (reboot) {
            setForm(initialForm);
          }
          e.target.dispatchEvent(
            new Event("reset", { bubbles: true, cancelable: true }),
          );
        }

        handleApiResponse(response);
      } catch (error: unknown) {
        if (error instanceof ApiError) {
          setError({
            statusCode: error.statusCode,
            message: error.message,
            name: error.name,
          });
        } else {
          const errObj = error as ApiError;
          handleApiResponse({
            success: false,
            data: null,
            message: `${errObj.name ?? "Unknown error"} ${errObj.stack ?? ""}`,
            totalResults: 0,
          });
        }
      }
    } else {
      handleApiResponse({
        success: false,
        data: null,
        message: "Error en la validación del formulario",
        totalResults: 0,
      });
    }
    setLoading(false);
  };

  return {
    form,
    loading,
    handleBlur,
    handleChange,
    handleChangeFile,
    handleSubmit,
    success,
    response: dataResult,
    errors: fieldErrors,
    message: apiMessage,
    setErrorsResponse,
    setForm,
  };
};
