import { toast } from "@heroui/react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useCallback, useEffect } from "react";
import { useParams } from "react-router";
import { OperationResponseColumns } from "../../components/column/OperationResponseColumns";
import { RolForm } from "../../components/form/RolForm";
import { LoadingComponent } from "../../components/spinner/LoadingComponent";
import { TableServer } from "../../components/table/TableServer";
import { getOperations } from "../../services/operationService";
import { getRolOperations } from "../../services/rolOperationService";
import { getRolById, updateRol } from "../../services/rolService";
import { useErrorsStore } from "../../stores/useErrorsStore";
import { useOperationStore } from "../../stores/useOperationStore";
import { customStyles } from "../../theme/tableTheme";
import type { ApiResponse } from "../../types/ApiResponse";
import type { RolOperationResponse } from "../../types/RolOperationResponse";
import type { RolRequest } from "../../types/RolRequest";
import type { RolResponse } from "../../types/RolResponse";
import type { ApiError } from "../../types/errors";
import { validationFailureToString } from "../../utils/converted";

export function UpdateRolPage() {
  const { setError } = useErrorsStore();
  const { filters, setFilters } = useOperationStore();
  const { id } = useParams();
  const client = useQueryClient();

  const {
    data: rolToUpdate,
    isLoading,
    error,
  } = useQuery<ApiResponse<RolResponse>, ApiError>({
    queryKey: ["rolToUpdate", id],
    queryFn: () => getRolById(Number(id)),
  });

  const { error: operationsError } = useQuery<
    ApiResponse<RolOperationResponse[]>,
    ApiError
  >({
    queryKey: ["operationsForRol", id],
    queryFn: () =>
      getRolOperations({
        filters: `RolId:eq:${id}`,
        pageNumber: 1,
        pageSize: 1000,
        include: null,
        includeTotal: false,
      }),
  });

  const onSubmit = useCallback(
    async (form: RolRequest) => {
      form.createdBy = null;
      form.updatedBy = null;
      const response = await updateRol(form);

      if (!response.success) {
        toast.danger(
          `${response.message} ${validationFailureToString(response.data)}`,
        );
        return response;
      }

      await client.invalidateQueries({ queryKey: ["roles"] });
      await client.invalidateQueries({ queryKey: ["rolToUpdate", id] });

      toast.success("Rol actualizado correctamente");

      return response;
    },
    [client, id],
  );

  const queryFn = useCallback(
    async (filters: string, page: number, pageSize: number) => {
      return getOperations({
        pageNumber: page,
        pageSize,
        filters,
        include: "Module",
        includeTotal: false,
      });
    },
    [],
  );

  useEffect(() => {
    if (operationsError) {
      setError(operationsError);
    }
    if (error) {
      setError(error);
    }
  }, [operationsError, error, setError]);

  if (isLoading) {
    return <LoadingComponent />;
  }

  return (
    <div>
      {rolToUpdate?.success && rolToUpdate.data ? (
        <RolForm
          initialForm={rolToUpdate.data}
          type="edit"
          onSubmit={onSubmit}
        />
      ) : (
        <div>
          Error: {error instanceof Error ? error.message : "Unknown error"}
        </div>
      )}
      <h2 className="text-xl font-bold text-center">
        Administrar Permisos de Rol
      </h2>
      <TableServer
        hasFilters
        columns={OperationResponseColumns}
        filters={filters}
        queryFn={queryFn}
        queryKey={`permissionsByRol-${id}`}
        setFilters={setFilters}
        styles={customStyles}
        text="permisos"
        title="Permisos del Rol"
      />
    </div>
  );
}
