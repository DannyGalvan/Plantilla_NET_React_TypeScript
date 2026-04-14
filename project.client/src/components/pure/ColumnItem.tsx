import { Switch } from "@heroui/react";
import { useCallback } from "react";
import type { TableColumn } from "react-data-table-component";

interface ColumnItemProps<T> {
  readonly column: TableColumn<T>;
  readonly changeVisibilitiColumn: (column: TableColumn<T>) => void;
}

export function ColumnItem<T>({
  column,
  changeVisibilitiColumn,
}: ColumnItemProps<T>) {
  const handleChange = useCallback(() => {
    changeVisibilitiColumn(column);
  }, [changeVisibilitiColumn, column]);

  return (
    <div key={`column-${column.id}`} className="flex items-center">
      <Switch isSelected={!column.omit} onChange={handleChange}>
        {column.name}
      </Switch>
    </div>
  );
}
