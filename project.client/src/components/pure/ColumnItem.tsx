import { Label, Switch } from "@heroui/react";
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
    <div
      key={`column-${column.id}`}
      className="flex items-center justify-between w-full p-2 border-b border-default-100 last:border-b-0"
    >
      <div className="flex w-full">
        <Switch
          className="flex gap-3 hover:opacity-80 transition-opacity"
          isSelected={!column.omit}
          onChange={handleChange}
        >
          <Switch.Control>
            <Switch.Thumb />
          </Switch.Control>
          <Label className="cursor-pointer">{column.name as string}</Label>
        </Switch>
      </div>
    </div>
  );
}
