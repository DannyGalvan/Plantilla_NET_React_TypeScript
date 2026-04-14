import { toast } from "@heroui/react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useCallback } from "react";
import { useParams } from "react-router";
import { RolForm } from "../../components/form/RolForm";
import { LoadingComponent } from "../../components/spinner/LoadingComponent";
import { getRolById, updateRol } from "../../services/rolService";
import type { RolRequest } from "../../types/RolRequest";
import { validationFailureToString } from "../../utils/converted";

export function UpdateRolPage() {
  const { id } = useParams();
  const client = useQueryClient();

  const {
    data: rolToUpdate,
    isLoading,
    error,
  } = useQuery({
    queryKey: ["rolToUpdate", id],
    queryFn: () => getRolById(Number(id)),
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
    </div>
  );
}
