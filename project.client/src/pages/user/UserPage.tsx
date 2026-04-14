import { useCallback } from "react";
import { UserResponseColumns } from "../../components/column/UserResponseColumns";
import { TableServer } from "../../components/table/TableServer";
import { getUsers } from "../../services/userService";
import { useUserStore } from "../../stores/useUserStore";
import { customStyles } from "../../theme/tableTheme";

export function UserPage() {
  const { filters, setFilters } = useUserStore();

  const queryFn = useCallback(
    async (filters: string, page: number, pageSize: number) => {
      return getUsers({
        pageNumber: page,
        pageSize,
        filters,
        include: "rol",
        includeTotal: false,
      });
    },
    [],
  );

  return (
    <div>
      <h1 className="text-2xl font-bold text-center mb-4">
        Listado de Usuarios
      </h1>
      <TableServer
        hasFilters
        columns={UserResponseColumns}
        filters={filters}
        queryFn={queryFn}
        queryKey="users"
        setFilters={setFilters}
        styles={customStyles}
        text="usuarios"
        title="Usuarios"
      />
    </div>
  );
}
