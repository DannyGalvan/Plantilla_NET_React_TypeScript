/* eslint-disable react/no-unused-prop-types */
import { Switch } from "@heroui/switch";
import { useCallback } from "react";
import type { TableColumn } from "react-data-table-component";
import { Icon } from "../icons/Icon";

interface ThumbIconProps {
  readonly isSelected: boolean;
  readonly className?: string;
}

interface ColumnItemProps<T> {
  readonly column: TableColumn<T>;
  readonly changeVisibilitiColumn: (column: TableColumn<T>) => void;
}

export function ColumnItem<T>({
  column,
  changeVisibilitiColumn,
}: ColumnItemProps<T>) {
  const thumbIcon = useCallback(
    ({ isSelected, className }: ThumbIconProps) =>
      isSelected ? (
        <Icon name={`bi bi-eye-fill ${className}`} />
      ) : (
        <Icon name={`bi bi-eye-slash-fill ${className}`} />
      ),
    [],
  );

  const handleChange = useCallback(() => {
    changeVisibilitiColumn(column);
  }, [changeVisibilitiColumn, column]);

  return (
    <div key={`column-${column.id}`} className="flex items-center">
      <Switch
        color="primary"
        isSelected={!column.omit}
        thumbIcon={thumbIcon}
        onChange={handleChange}
      >
        {column.name}
      </Switch>
    </div>
  );
}
