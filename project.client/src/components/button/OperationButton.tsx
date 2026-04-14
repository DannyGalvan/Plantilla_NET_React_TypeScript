import { Button, Spinner, toast } from "@heroui/react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useCallback, useMemo, useState } from "react";
import { useParams } from "react-router";
import {
  createRolOperation,
  getRolOperations,
  updateRolOperation,
} from "../../services/rolOperationService";
import type { ApiResponse } from "../../types/ApiResponse";
import type { OperationResponse } from "../../types/OperationResponse";
import type { RolOperationResponse } from "../../types/RolOperationResponse";
import { validationFailureToString } from "../../utils/converted";
import { Icon } from "../icons/Icon";

interface OperationButtonProps {
  readonly data: OperationResponse;
}

export function OperationButton({ data }: OperationButtonProps) {
  const { id } = useParams();
  const client = useQueryClient();
  const [isLoading, setIsLoading] = useState(false);

  const { data: operationsResponse } = useQuery<
    ApiResponse<RolOperationResponse[]>
  >({
    queryKey: ["operationsForRol", id],
    queryFn: async () =>
      getRolOperations({
        filters: `RolId:eq:${id}`,
        pageNumber: 1,
        pageSize: 1000,
        include: null,
        includeTotal: false,
      }),
    enabled: !!id,
  });

  const operations: RolOperationResponse[] = useMemo(
    () =>
      operationsResponse?.success && Array.isArray(operationsResponse.data)
        ? operationsResponse.data
        : [],
    [operationsResponse],
  );

  const hasPermission = operations.some(
    (operation: RolOperationResponse) => operation.operationId === data.id,
  );

  const handleSubmit = useCallback(async () => {
    setIsLoading(true);
    let response: ApiResponse<RolOperationResponse>;

    if (hasPermission) {
      const permission = operations.find(
        (operation: RolOperationResponse) => operation.operationId === data.id,
      )!;

      response = await updateRolOperation({
        id: permission.id,
        rolId: permission.rolId,
        operationId: permission.operationId,
        state: 0,
      });

      if (response.success) {
        await client.invalidateQueries({
          queryKey: ["operationsForRol", id],
        });

        toast.success(`Permiso ${data.name} removido correctamente`);

        setIsLoading(false);

        return;
      }

      toast.danger(
        `No se pudo remover el permiso: ${response.message} ${validationFailureToString(response.data)}`,
      );

      setIsLoading(false);

      return;
    }

    response = await createRolOperation({
      id: null,
      rolId: Number(id),
      operationId: data.id,
      state: 1,
    });

    if (response.success) {
      await client.invalidateQueries({
        queryKey: ["operationsForRol", id],
      });
      toast.success(`Permiso ${data.name} asignado correctamente`);
      setIsLoading(false);
      return;
    }

    toast.danger(
      `No se pudo asignar el permiso: ${response.message} ${validationFailureToString(response.data)}`,
    );
    setIsLoading(false);
  }, [data, hasPermission, operations, client, id]);

  return (
    <Button
      className={hasPermission ? "text-white" : ""}
      isPending={isLoading}
      size="sm"
      variant={hasPermission ? "danger" : "primary"}
      onPress={handleSubmit}
    >
      {({ isPending }) => (
        <>
          {isPending ? <Spinner color="current" size="sm" /> : null}
          {hasPermission ? (
            <Icon name="bi bi-trash" />
          ) : (
            <Icon name="bi bi-plus" />
          )}
          {hasPermission ? "Eliminar" : "Asignar"}
        </>
      )}
    </Button>
  );
}
