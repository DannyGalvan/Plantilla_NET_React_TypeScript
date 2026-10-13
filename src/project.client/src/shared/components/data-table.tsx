import {
  flexRender,
  getCoreRowModel,
  useReactTable,
  type ColumnDef,
  type RowData,
  type VisibilityState,
} from '@tanstack/react-table';
import {
  ArrowDownIcon,
  ArrowUpDownIcon,
  ArrowUpIcon,
  ChevronLeftIcon,
  ChevronRightIcon,
  Columns3Icon,
  SearchIcon,
} from 'lucide-react';
import { useEffect, useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router';

import type { ListParams, Paged } from '@/shared/api/crud-api';
import { cn } from '@/shared/lib/cn';
import {
  Button,
  DropdownMenu,
  DropdownMenuCheckboxItem,
  DropdownMenuContent,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  Input,
  NativeSelect,
  Skeleton,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/shared/ui';

import { EmptyState, QueryError } from './page';

declare module '@tanstack/react-table' {
  interface ColumnMeta<TData extends RowData, TValue> {
    /** Backend field used for server-side sorting (`sort=field` / `sort=-field`). */
    sortField?: string;
    className?: string;
    _types?: [TData, TValue];
  }
}

export interface TableState {
  page: number;
  pageSize: number;
  sort?: string;
  q?: string;
}

const PAGE_SIZES = [10, 20, 50, 100];

/**
 * Table state stored in the URL (shareable and survives reloads): page, pageSize, sort and the search text.
 */
export function useTableState(defaults: Partial<TableState> = {}) {
  const [params, setParams] = useSearchParams();
  const state: TableState = {
    page: Number(params.get('page') ?? defaults.page ?? 1),
    pageSize: Number(params.get('pageSize') ?? defaults.pageSize ?? 20),
    sort: params.get('sort') ?? defaults.sort,
    q: params.get('q') ?? undefined,
  };

  const update = (changes: Partial<TableState>) => {
    setParams(
      (current) => {
        const next = new URLSearchParams(current);
        const merged = {
          ...state,
          ...changes,
          page:
            changes.page ??
            (changes.q !== undefined || changes.sort !== undefined || changes.pageSize ? 1 : state.page),
        };
        for (const [key, value] of Object.entries(merged)) {
          if (value === undefined || value === '' || value === null) {
            next.delete(key);
          } else {
            next.set(key, String(value));
          }
        }

        return next;
      },
      { replace: true },
    );
  };

  return [state, update] as const;
}

/** Converts the table state to API list parameters; `search` builds the filter for the search box. */
export function toListParams(state: TableState, search?: (q: string) => string, extraFilter?: string): ListParams {
  const filters = [state.q ? search?.(state.q) : undefined, extraFilter].filter(Boolean);
  return {
    page: state.page,
    pageSize: state.pageSize,
    sort: state.sort,
    filter: filters.length ? filters.join(';') : undefined,
  };
}

interface DataTableProps<T> {
  id: string;
  columns: ColumnDef<T, unknown>[];
  data?: Paged<T>;
  isLoading: boolean;
  error?: unknown;
  onRetry?: () => void;
  state: TableState;
  onStateChange: (changes: Partial<TableState>) => void;
  searchable?: boolean;
  toolbar?: ReactNode;
  emptyTitle?: string;
}

function loadVisibility(id: string): VisibilityState {
  try {
    return JSON.parse(localStorage.getItem(`table:${id}:columns`) ?? '{}') as VisibilityState;
  } catch {
    return {};
  }
}

/** Server-side table: paging, sorting and search live in the URL; column visibility is remembered per table. */
export function DataTable<T>({
  id,
  columns,
  data,
  isLoading,
  error,
  onRetry,
  state,
  onStateChange,
  searchable = true,
  toolbar,
  emptyTitle,
}: DataTableProps<T>) {
  const { t } = useTranslation();
  const [visibility, setVisibility] = useState<VisibilityState>(() => loadVisibility(id));
  const [search, setSearch] = useState(state.q ?? '');

  useEffect(() => {
    try {
      localStorage.setItem(`table:${id}:columns`, JSON.stringify(visibility));
    } catch {
      // Ignore blocked storage.
    }
  }, [id, visibility]);

  useEffect(() => {
    const handle = setTimeout(() => {
      if ((state.q ?? '') !== search) {
        onStateChange({ q: search || undefined });
      }
    }, 350);
    return () => clearTimeout(handle);
  }, [search, state.q, onStateChange]);

  const table = useReactTable({
    data: data?.items ?? [],
    columns,
    getCoreRowModel: getCoreRowModel(),
    manualPagination: true,
    manualSorting: true,
    state: { columnVisibility: visibility },
    onColumnVisibilityChange: setVisibility,
  });

  const toggleSort = (field: string) => {
    const next = state.sort === field ? `-${field}` : state.sort === `-${field}` ? undefined : field;
    onStateChange({ sort: next });
  };

  const totalPages = data?.totalPages ?? 1;

  return (
    <div className="space-y-3">
      <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
        {searchable ? (
          <div className="relative w-full sm:max-w-xs">
            <SearchIcon
              className="pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2 text-muted-foreground"
              aria-hidden
            />
            <Input
              aria-label={t('actions.search')}
              placeholder={t('actions.search')}
              className="pl-8"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
            />
          </div>
        ) : (
          <span />
        )}
        <div className="flex items-center gap-2">
          {toolbar}
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="outline" size="sm">
                <Columns3Icon />
                {t('table.columns')}
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuLabel>{t('table.columns')}</DropdownMenuLabel>
              <DropdownMenuSeparator />
              {table
                .getAllLeafColumns()
                .filter((column) => column.getCanHide())
                .map((column) => (
                  <DropdownMenuCheckboxItem
                    key={column.id}
                    checked={column.getIsVisible()}
                    onCheckedChange={(value) => column.toggleVisibility(Boolean(value))}
                  >
                    {typeof column.columnDef.header === 'string' ? column.columnDef.header : column.id}
                  </DropdownMenuCheckboxItem>
                ))}
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </div>

      {error ? (
        <QueryError error={error} onRetry={onRetry} />
      ) : (
        <div className="rounded-lg border bg-card">
          <Table>
            <TableHeader>
              {table.getHeaderGroups().map((group) => (
                <TableRow key={group.id}>
                  {group.headers.map((header) => {
                    const field = header.column.columnDef.meta?.sortField;
                    const direction = state.sort === field ? 'asc' : state.sort === `-${field}` ? 'desc' : undefined;
                    const label = header.isPlaceholder
                      ? null
                      : flexRender(header.column.columnDef.header, header.getContext());
                    return (
                      <TableHead
                        key={header.id}
                        className={header.column.columnDef.meta?.className}
                        aria-sort={direction === 'asc' ? 'ascending' : direction === 'desc' ? 'descending' : undefined}
                      >
                        {field ? (
                          <button
                            type="button"
                            className="inline-flex items-center gap-1 hover:text-foreground"
                            onClick={() => toggleSort(field)}
                          >
                            {label}
                            {direction === 'asc' ? (
                              <ArrowUpIcon className="size-3.5" />
                            ) : direction === 'desc' ? (
                              <ArrowDownIcon className="size-3.5" />
                            ) : (
                              <ArrowUpDownIcon className="size-3.5 opacity-50" />
                            )}
                          </button>
                        ) : (
                          label
                        )}
                      </TableHead>
                    );
                  })}
                </TableRow>
              ))}
            </TableHeader>
            <TableBody>
              {isLoading
                ? Array.from({ length: 5 }, (_, row) => (
                    <TableRow key={row}>
                      {table.getVisibleLeafColumns().map((column) => (
                        <TableCell key={column.id}>
                          <Skeleton className="h-4 w-full max-w-40" />
                        </TableCell>
                      ))}
                    </TableRow>
                  ))
                : table.getRowModel().rows.map((row) => (
                    <TableRow key={row.id}>
                      {row.getVisibleCells().map((cell) => (
                        <TableCell key={cell.id} className={cell.column.columnDef.meta?.className}>
                          {flexRender(cell.column.columnDef.cell, cell.getContext())}
                        </TableCell>
                      ))}
                    </TableRow>
                  ))}
            </TableBody>
          </Table>
          {!isLoading && data?.items.length === 0 && (
            <div className="p-4">
              <EmptyState title={emptyTitle ?? t('table.noResults')} />
            </div>
          )}
        </div>
      )}

      <div className="flex flex-col items-center justify-between gap-2 text-sm text-muted-foreground sm:flex-row">
        <span>{data?.total != null ? t('table.total', { count: data.total }) : ''}</span>
        <div className="flex items-center gap-2">
          <label className="flex items-center gap-2">
            <span>{t('table.rowsPerPage')}</span>
            <NativeSelect
              className="h-8 w-20"
              value={state.pageSize}
              onChange={(event) => onStateChange({ pageSize: Number(event.target.value) })}
            >
              {PAGE_SIZES.map((size) => (
                <option key={size} value={size}>
                  {size}
                </option>
              ))}
            </NativeSelect>
          </label>
          <span>{t('table.pageOf', { page: state.page, pages: Math.max(totalPages, 1) })}</span>
          <Button
            variant="outline"
            size="icon"
            className={cn('size-8')}
            aria-label={t('table.previous')}
            disabled={state.page <= 1}
            onClick={() => onStateChange({ page: state.page - 1 })}
          >
            <ChevronLeftIcon />
          </Button>
          <Button
            variant="outline"
            size="icon"
            className="size-8"
            aria-label={t('table.next')}
            disabled={state.page >= totalPages}
            onClick={() => onStateChange({ page: state.page + 1 })}
          >
            <ChevronRightIcon />
          </Button>
        </div>
      </div>
    </div>
  );
}
