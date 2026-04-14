import { useQuery } from "@tanstack/react-query";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import DataTable from "react-data-table-component";

import { PAGINATION_OPTIONS, SELECTED_MESSAGE } from "../../configs/constants";
import { useToggle } from "../../hooks/useToggle";
import { useErrorsStore } from "../../stores/useErrorsStore";

import { customStyles } from "../../theme/tableTheme";
import type { ApiResponse } from "../../types/ApiResponse";

import { useRangeOfDatesStore } from "../../stores/useRangeOfDatesStore";
import type { ApiError } from "../../types/errors";
import type { ListFilter } from "../../types/ListFilter.ts";
import type { TableColumnWithFilters } from "../../types/TableColumnWithFilters.ts";
import { TableSearch } from "../form/TableSearch";
import { InputDateSelector } from "../input/InputDateSelector";
import { MesajeNoData } from "../messages/MesajeNoData";
import { ModalTable } from "../modal/ModalTable";
import { SubHeaderTableButton } from "../pure/SubHeaderTableButton";
import { LoadingComponent } from "../spinner/LoadingComponent";

export interface TableServerProps<T> {
  readonly columns: TableColumnWithFilters<T>[];
  readonly queryKey: string;
  readonly filters: ListFilter;
  readonly setFilters: (filters: ListFilter) => void;
  readonly queryFn: (
    filters: string,
    page: number,
    pageSize: number,
  ) => Promise<ApiResponse<T[]>>;
  readonly title: string;
  readonly text: string;
  readonly styles: object;
  readonly isExternalLoading?: boolean;
  readonly hasFilters?: boolean;
  readonly hasRangeOfDates?: boolean;
  readonly fieldRangeOfDates?: string;
  readonly width?: boolean;
  readonly selectedRows?: boolean;
  readonly onSelectedRowsChange?: (state: {
    allSelected: boolean;
    selectedCount: number;
    selectedRows: T[];
  }) => void;
}

export function TableServer<T extends object>({
  queryKey,
  queryFn,
  columns,
  text,
  title,
  width,
  filters,
  setFilters,
  hasFilters = true,
  hasRangeOfDates = false,
  fieldRangeOfDates,
  selectedRows,
  onSelectedRowsChange,
  styles,
  isExternalLoading = false,
}: TableServerProps<T>) {
  const { open, toggle } = useToggle();
  const { setError } = useErrorsStore();
  const { end, start, getDateFilters } = useRangeOfDatesStore();
  const [field, setField] = useState<TableColumnWithFilters<T> | undefined>(
    undefined,
  );
  const [cols, setCols] = useState(columns);
  const searchField = useRef<HTMLInputElement>(null);

  const {
    data,
    isPending,
    error: apiError,
  } = useQuery<ApiResponse<T[]>, ApiError>({
    queryKey: [
      queryKey,
      filters.filter,
      hasRangeOfDates ? end : "",
      hasRangeOfDates ? start : "",
      filters.page,
      filters.pageSize,
      hasRangeOfDates,
      fieldRangeOfDates,
    ],
    queryFn: () =>
      queryFn(
        hasRangeOfDates
          ? `${fieldRangeOfDates ? getDateFilters(fieldRangeOfDates) : ""}${filters.filter ? ` AND ${filters.filter}` : ""}`
          : `${filters.filter ? `${filters.filter}` : ""}`,
        filters.page,
        filters.pageSize,
      ),
  });

  const changeVisibilitiColumn = useCallback(
    (column: TableColumnWithFilters<T>) => {
      column.omit = !column.omit;
      const cols = columns.map((col) => (col.id === column.id ? column : col));
      setCols(cols);
    },
    [columns],
  );

  const selectedField = useCallback(
    (e: React.ChangeEvent<HTMLSelectElement>) => {
      setField(columns.find((col) => col.id === e.target.value));
    },
    [columns],
  );

  const filterData = useCallback(() => {
    const searchValue = searchField.current?.value;
    const filter =
      field && field.filterField ? field.filterField(searchValue) : "";
    setFilters({
      ...filters,
      filter,
    });
  }, [field, filters, setFilters]);

  const onChangePage = useCallback(
    (page: number) => {
      setFilters({
        ...filters,
        page,
      });
    },
    [filters, setFilters],
  );

  const onChangeRowsPerPage = useCallback(
    (rowsPerPage: number, currentPage: number) => {
      setFilters({
        ...filters,
        pageSize: rowsPerPage,
        page: currentPage,
      });
    },
    [filters, setFilters],
  );

  const memoizedColumns = useMemo(() => cols, [cols]);

  useEffect(() => {
    if (apiError) {
      setError({
        statusCode: apiError.statusCode,
        message: apiError.message,
        name: apiError.name,
      });
    }
  }, [apiError, setError]);

  return (
    <div className="w-full">
      {hasRangeOfDates ? (
        <InputDateSelector label="Filtro de Rango de Fechas" />
      ) : null}
      {hasFilters ? (
        <TableSearch
          columns={columns}
          filterData={filterData}
          searchField={searchField}
          selectedField={selectedField}
        />
      ) : null}
      <div className="min-h-[495px] overflow-auto">
        <DataTable
          fixedHeader
          highlightOnHover
          pagination
          paginationServer
          pointerOnHover
          responsive
          striped
          subHeader
          subHeaderWrap
          clearSelectedRows={selectedRows}
          columns={memoizedColumns}
          contextMessage={SELECTED_MESSAGE}
          customStyles={styles ?? customStyles}
          data={data?.success ? data?.data : []}
          expandableRows={width}
          fixedHeaderScrollHeight="325px"
          noDataComponent={
            <MesajeNoData mesaje={`No se encontraros datos ${text}`} />
          }
          paginationComponentOptions={PAGINATION_OPTIONS}
          paginationDefaultPage={filters.page}
          paginationTotalRows={data?.totalResults}
          progressComponent={<LoadingComponent />}
          progressPending={isPending || isExternalLoading}
          selectableRows={selectedRows}
          subHeaderComponent={<SubHeaderTableButton onClick={toggle} />}
          theme="individuality"
          title={title}
          onChangePage={onChangePage}
          onChangeRowsPerPage={onChangeRowsPerPage}
          onSelectedRowsChange={onSelectedRowsChange}
        />
      </div>
      <ModalTable
        changeVisibilitiColumn={changeVisibilitiColumn}
        columns={columns}
        open={open}
        toggle={toggle}
      />
    </div>
  );
}
