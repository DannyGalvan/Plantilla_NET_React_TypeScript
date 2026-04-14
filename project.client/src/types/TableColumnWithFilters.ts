import type { TableColumn } from "react-data-table-component";

export type TableColumnWithFilters<T> =
  | (TableColumn<T> & {
      hasFilter: true;
      filterField: (value: string | undefined) => string;
    })
  | (TableColumn<T> & { hasFilter?: false; filterField?: never });
