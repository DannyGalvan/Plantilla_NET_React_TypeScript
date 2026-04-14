import { ZodError } from "zod";
import type { ErrorObject } from "../hooks/useForm";
import type { Authorizations } from "../types/Authorizations";
import type { Operations } from "../types/Operations";
import type { ValidationFailure } from "../types/ValidationFailure";

export const toCamelCase = (inputString: string) => {
  return inputString.replace(
    /(\w)(\w*)/g,
    function (_match, firstChar, restOfString) {
      return firstChar.toLowerCase() + restOfString;
    },
  );
};

export const dataFormatter = (number: number) =>
  `Q${Intl.NumberFormat("en").format(number).toString()}`;

export const toAllOperations = (operations: Authorizations[]) => {
  const allOperations: Operations[] = [];

  operations.forEach((op) => {
    allOperations.push(...op.operations);
  });

  return allOperations;
};

export const toFormatTime = (date: string) => {
  // Crear un objeto Date con la fecha
  const tempDate: Date = new Date(date);

  // Obtener la hora, minutos y segundos
  const hours: number = tempDate.getHours();
  const minutes: number = tempDate.getMinutes();

  // Formatear la hora si es necesario (por ejemplo, agregar ceros delante si es menor que 10)
  const formatHour: string = hours < 10 ? "0" + hours : hours.toString();
  const formatMinute: string =
    minutes < 10 ? "0" + minutes : minutes.toString();

  // Crear una cadena de texto con la hora
  const formatHours = `${formatHour}:${formatMinute}`;

  // Mostrar la hora
  return formatHours;
};

export const toFormatDate = (date: string) => {
  const dateTemp = date.length === 10 ? date + "T06:00:00" : date;

  // Crear un objeto Date con la fecha
  const fecha: Date = new Date(dateTemp);

  // Obtener el día, mes y año
  const dia: number = fecha.getDate();
  const mes: number = fecha.getMonth() + 1; // Los meses en JavaScript son base 0, por lo que se agrega 1
  const año: number = fecha.getFullYear();

  // Formatear la fecha si es necesario (por ejemplo, agregar ceros delante si es menor que 10)
  const diaFormateado: string = dia < 10 ? "0" + dia : dia.toString();
  const mesFormateado: string = mes < 10 ? "0" + mes : mes.toString();

  // Crear una cadena de texto con la fecha
  const fechaFormateada = `${año}-${mesFormateado}-${diaFormateado}`;

  // Mostrar la fecha
  return fechaFormateada;
};

export const dateNow = () => {
  const now = new Date();
  const offset = now.getTimezoneOffset(); // Obtiene la diferencia en minutos entre UTC y la zona horaria local
  now.setMinutes(now.getMinutes() - offset); // Ajusta la fecha restando la diferencia

  const formattedDate = now.toISOString().split("T")[0];
  return formattedDate;
};

export const today = () => {
  const now = new Date();

  return now;
};

export const minDateMaxDate = (months = 6) => {
  const sixMonthsAgo = new Date();
  sixMonthsAgo.setMonth(sixMonthsAgo.getMonth() - months);

  const minDate = sixMonthsAgo.toISOString().substring(0, 10);
  const maxDate = new Date().toISOString().substring(0, 10);

  return { minDate, maxDate };
};

export const handleOneLevelZodError = ({ issues }: ZodError<unknown>) => {
  const formData: ErrorObject = {};

  issues.forEach(({ path, message }) => {
    formData[path.join("-")] = message;
  });

  return formData;
};

// export const hasJsonOrOtherToString = (object: any, func: Selector<any>) => {
//   return typeof func(object) === "object"
//     ? JSON.stringify({ ...object }).toLowerCase()
//     : func(object)?.toString().toLowerCase();
// };

export const copyToClipboard = async (textToCopy: string) => {
  try {
    await navigator.clipboard.writeText(textToCopy);
  } catch (err) {
    console.error("Error al copiar el texto: ", err);
  }
};

export const validationFailureToString = (
  errors: ValidationFailure[] | null,
) => {
  if (!errors) return "";

  return errors
    .map((error) => `${error.propertyName}: ${error.errorMessage}`)
    .join(", ");
};

export const errorObjectToString = (errorObject?: ErrorObject) => {
  if (!errorObject) return "";

  return Object.entries(errorObject)
    .map(
      ([key, value]) =>
        `${key}: ${Array.isArray(value) ? value.join(", ") : value}`,
    )
    .join(", ");
};

export const mapValidationFailuresToFieldErrors = (
  errors: ValidationFailure[] | null,
) => {
  const errorsConverted: ErrorObject = {};

  if (errors == null) {
    return errorsConverted;
  }

  errors.forEach((error) => {
    errorsConverted[toCamelCase(error.propertyName)] = error.errorMessage;
  });

  if (Object.keys(errorsConverted).length !== 0) {
    return errorsConverted;
  }
};

export const constructFilters = (filters: string) => {
  if (filters.startsWith(" AND ")) {
    filters = filters.slice(5); // Eliminar el prefijo " AND " si existe
  }

  return filters;
};

type SimpleValue = string | number | boolean | null | Date;
type FileLike = File | Blob;

const isFileLike = (v: unknown): v is FileLike =>
  v instanceof File || v instanceof Blob;

const isSimple = (v: unknown): v is SimpleValue =>
  typeof v === "string" ||
  typeof v === "number" ||
  typeof v === "boolean" ||
  v === null ||
  v instanceof Date;

const isPlainObject = (v: unknown): v is Record<string, unknown> =>
  typeof v === "object" && v !== null && !Array.isArray(v) && !isFileLike(v);

function appendSimple(form: FormData, key: string, value: SimpleValue): void {
  if (value === null) return; // omite nulos
  if (value instanceof Date) {
    form.append(key, value.toISOString());
  } else {
    form.append(key, String(value));
  }
}

/**
 * Convierte un objeto arbitrario a FormData.
 * - Arrays de archivos -> misma clave repetida (sin índices)
 * - Otros arrays -> claves indexadas (key[0], key[1], ...)
 * - Objetos anidados -> recursivo con "namespace.key"
 */
export function customToFormData(
  obj: Record<string, unknown>,
  form: FormData = new FormData(),
  namespace = "",
): FormData {
  for (const key of Object.keys(obj)) {
    const value = obj[key];
    if (value === undefined || value === null) continue;

    const formKey = namespace ? `${namespace}.${key}` : key;

    if (isFileLike(value)) {
      // Archivo suelto
      form.append(formKey, value);
      continue;
    }

    if (Array.isArray(value)) {
      // ¿Array 100% de archivos?
      if (value.every(isFileLike)) {
        for (const item of value) {
          // misma clave repetida
          form.append(formKey, item);
        }
      } else {
        // Array mixto (objetos, simples, y/o archivos sueltos)
        value.forEach((item, index) => {
          const arrayKey = `${formKey}[${index}]`;
          if (isFileLike(item)) {
            // por compatibilidad, repetimos la clave base (también puedes usar arrayKey)
            form.append(formKey, item);
          } else if (isPlainObject(item)) {
            customToFormData(item, form, arrayKey);
          } else if (isSimple(item)) {
            appendSimple(form, arrayKey, item);
          }
          // items no compatibles se omiten
        });
      }
      continue;
    }

    if (isPlainObject(value)) {
      customToFormData(value, form, formKey);
      continue;
    }

    if (isSimple(value)) {
      appendSimple(form, formKey, value);
      continue;
    }
  }

  return form;
}
