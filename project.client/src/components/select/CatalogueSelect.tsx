import { useCallback } from "react";
import type { MultiValue, SelectInstance, SingleValue } from "react-select";
import AsyncSelect from "react-select/async";
import { getCatalogue } from "../../services/catalogueService";
import type { ApiResponse } from "../../types/ApiResponse";
import type { filterOptions } from "../../types/FilterTypes";

interface BaseCatalogueSelectProps {
  label: string;
  defaultValue?: { label: string; value: string } | null;
  name?: string;
  placeholder?: string;
  deps?: string;
  cacheOptions?: boolean;
  defaultOptions?: boolean;
  errorMessage?: string;
  isMulti?: boolean;
  isInvalid?: boolean;
  isRequired?: boolean;
  ref?: React.Ref<SelectInstance<{ label: string; value: string }>>;
  onChange?: (
    option:
      | SingleValue<{ label: string; value: string }>
      | MultiValue<{ label: string; value: string }>
      | null,
  ) => void;
}

// Opción A: `catalogue` está presente → `queryFn` y `selectorFn` son opcionales
interface CatalogueOption<T> extends BaseCatalogueSelectProps {
  catalogue: string;
  fieldSearch?: string;
  queryFn?: (options: filterOptions) => Promise<ApiResponse<T[]>>;
  selectorFn?: (item: T) => { label: string; value: string };
}

// Opción B: no hay `catalogue` → `queryFn` y `selectorFn` son obligatorios
interface CustomOption<T> extends BaseCatalogueSelectProps {
  catalogue?: undefined;
  fieldSearch: string;
  queryFn: (options: filterOptions) => Promise<ApiResponse<T[]>>;
  selectorFn: (item: T) => { label: string; value: string };
}

// Unión de ambas opciones válidas
export type CatalogueSelectProps<T> = CatalogueOption<T> | CustomOption<T>;

const constructQuery = (
  inputValue: string,
  deps: string,
  fieldSearch: string,
): string => {
  let value = "";

  if (inputValue) {
    value = `${fieldSearch}:like:${inputValue}`;
  }

  if (deps) {
    value += ` AND ${deps}`;
  }

  if (value.startsWith(" AND ")) {
    value = value.slice(5); // Eliminar el prefijo " AND " si existe
  }

  return value;
};

export function CatalogueSelect<T extends object>({
  catalogue,
  label,
  defaultValue,
  name,
  placeholder = "Selecciona una opción",
  deps,
  queryFn,
  selectorFn,
  fieldSearch,
  onChange,
  cacheOptions = true,
  defaultOptions = true,
  errorMessage = "",
  isMulti = false,
  isInvalid = false,
  isRequired = false,
  ref,
}: CatalogueSelectProps<T>) {
  const catalogueOptions = useCallback(
    async (inputValue: string) => {
      if (!queryFn && catalogue) {
        const response = await getCatalogue({
          filters: constructQuery(inputValue, deps || "", "Name"),
          include: null,
          includeTotal: false,
          pageNumber: 1,
          pageSize: 100,
          catalogue: catalogue,
        });
        return response?.success
          ? response.data.map((item) => ({
              label: item.name,
              value: String(item.id),
            }))
          : [];
      } else {
        const response = await queryFn!({
          filters: constructQuery(
            inputValue,
            deps || "",
            fieldSearch || "Name",
          ),
          include: null,
          includeTotal: false,
          pageNumber: 1,
          pageSize: 100,
        });

        return response?.success
          ? response.data.map((item) =>
              selectorFn
                ? selectorFn(item)
                : {
                    label: "label",
                    value: "item",
                  },
            )
          : [];
      }
    },
    [catalogue, deps, queryFn, selectorFn, fieldSearch],
  );

  const loadingMessage = useCallback(
    (obj: { inputValue: string }) => <div>Cargando {obj.inputValue}...</div>,
    [],
  );

  const noOptionsMessage = useCallback(() => {
    return "No hay opciones disponibles";
  }, []);

  return (
    <div>
      <div
        className={`rounded-xl transition-colors ${
          isInvalid
            ? "bg-danger-50 hover:bg-danger-100"
            : "bg-default-100 hover:bg-default-200"
        }`}
      >
        <label
          className={`${isInvalid ? "text-danger" : "text-default-500"} text-xs ms-3 pt-2 block`}
          htmlFor={label}
        >
          {label}{" "}
          {isRequired && <span className="text-danger font-bold ml-1">*</span>}
        </label>
        <AsyncSelect
          key={deps}
          ref={ref}
          backspaceRemovesValue
          isClearable
          required
          tabSelectsValue
          cacheOptions={cacheOptions}
          classNamePrefix="react-select"
          defaultOptions={defaultOptions}
          defaultValue={defaultValue || null}
          isMulti={isMulti}
          loadOptions={catalogueOptions}
          loadingMessage={loadingMessage}
          name={name}
          noOptionsMessage={noOptionsMessage}
          placeholder={placeholder}
          styles={{
            control: (base) => ({
              ...base,
              borderRadius: "0.5rem",
              borderColor: "transparent",
              boxShadow: "none",
              backgroundColor: "transparent",
              minHeight: "40px",
              cursor: "pointer",
              ":hover": {
                ...base[":hover"],
                borderColor: "transparent",
              },
            }),
            menu: (base) => ({
              ...base,
              borderRadius: "0.5rem",
              backgroundColor: "hsl(var(--heroui-content1))",
              boxShadow: "0px 4px 20px rgba(0, 0, 0, 0.15)",
              zIndex: 9999,
            }),
            menuList: (base) => ({
              ...base,
              zIndex: 9999,
              maxHeight: "150px",
              overflowY: "auto",
              padding: "0.5rem",
            }),
            option: (base, state) => ({
              ...base,
              backgroundColor: state.isSelected
                ? "hsl(var(--heroui-primary))"
                : state.isFocused
                  ? "hsl(var(--heroui-default-200))"
                  : "transparent",
              color: state.isSelected
                ? "hsl(var(--heroui-primary-foreground))"
                : "hsl(var(--heroui-foreground))",
              borderRadius: "0.375rem",
              cursor: "pointer",
              ":active": {
                backgroundColor: "hsl(var(--heroui-primary-200))",
              },
            }),
            placeholder: (base) => ({
              ...base,
              color: isInvalid
                ? "hsl(var(--heroui-danger))"
                : "hsl(var(--heroui-default-500))",
            }),
            singleValue: (base) => ({
              ...base,
              color: "hsl(var(--heroui-foreground))",
            }),
            multiValue: (base) => ({
              ...base,
              backgroundColor: "hsl(var(--heroui-default-200))",
              borderRadius: "0.375rem",
            }),
            multiValueLabel: (base) => ({
              ...base,
              color: "hsl(var(--heroui-foreground))",
            }),
            multiValueRemove: (base) => ({
              ...base,
              color: "hsl(var(--heroui-foreground-500))",
              ":hover": {
                backgroundColor: "hsl(var(--heroui-danger-100))",
                color: "hsl(var(--heroui-danger))",
              },
            }),
            input: (base) => ({
              ...base,
              color: "hsl(var(--heroui-foreground))",
            }),
          }}
          onChange={onChange}
        />
      </div>
      <p className="text-danger text-sm ms-1 mt-1">{errorMessage}</p>
    </div>
  );
}
