import { useCallback } from "react";
import Select, {
  type ActionMeta,
  type GroupBase,
  type MultiValue,
  type OptionsOrGroups,
  type SelectInstance,
  type SingleValue,
} from "react-select";

export type OptionValue =
  | SingleValue<{ label: string; value: string }>
  | MultiValue<{ label: string; value: string }>
  | null;

type Option = { label: string; value: string };

interface OptionSelectProps {
  readonly label: string;
  readonly name: string;
  readonly options: OptionsOrGroups<Option, GroupBase<Option>>;
  readonly defaultValue?: OptionValue;
  readonly isMulti?: boolean;
  readonly isInvalid?: boolean;
  readonly errorMessage?: string;
  readonly placeholder?: string;
  readonly isReadOnly?: boolean;
  readonly ref?: React.Ref<SelectInstance<{ label: string; value: string }>>;
  readonly onChange?:
    | ((newValue: OptionValue, actionMeta: ActionMeta<OptionValue>) => void)
    | undefined;
  readonly isRequired?: boolean;
}

export function OptionsSelect({
  label,
  name,
  options,
  defaultValue,
  isMulti = false,
  isInvalid = false,
  isReadOnly = false,
  isRequired = false,
  errorMessage = "",
  placeholder = "Select an option",
  onChange,
  ref,
}: OptionSelectProps) {
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
        <Select
          ref={ref}
          backspaceRemovesValue
          isClearable
          tabSelectsValue
          classNamePrefix="react-select"
          defaultValue={defaultValue}
          isDisabled={isReadOnly}
          isMulti={isMulti}
          name={name}
          noOptionsMessage={noOptionsMessage}
          options={options}
          placeholder={placeholder}
          required={isRequired}
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
