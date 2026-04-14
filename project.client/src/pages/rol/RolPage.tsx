import { useCallback } from "react";
import { RolResponseColumns } from "../../components/column/RolResponseColumns";
import { TableServer } from "../../components/table/TableServer";
import { getRoles } from "../../services/rolService";
import { useRolStore } from "../../stores/useRolStore";
import { customStyles } from "../../theme/tableTheme";

export function RolPage() {
  const { filters, setFilters } = useRolStore();

  const queryFn = useCallback(
    async (filters: string, page: number, pageSize: number) => {
      return getRoles({
        pageNumber: page,
        pageSize,
        filters,
        include: "",
        includeTotal: false,
      });
    },
    [],
  );

  return (
    <div>
      <h1 className="text-2xl font-bold text-center mb-4">Listado de Roles</h1>
      <TableServer
        hasFilters
        columns={RolResponseColumns}
        filters={filters}
        queryFn={queryFn}
        queryKey="roles"
        setFilters={setFilters}
        styles={customStyles}
        text="roles"
        title="Roles"
      />
    </div>
  );
}
