import { toast } from "@heroui/react";
import { useQueryClient } from "@tanstack/react-query";
import { useCallback } from "react";
import { RolForm } from "../../components/form/RolForm";
import { createRol } from "../../services/rolService";
import type { RolRequest } from "../../types/RolRequest";

export function CreateRolPage() {
  const client = useQueryClient();

  const initialData: RolRequest = {
    name: "",
    description: "",
    state: 1,
  };

  const onSubmit = useCallback(
    async (form: RolRequest) => {
      const response = await createRol(form);

      if (!response.success) {
        toast.danger(response.message);
        return response;
      }

      await client.invalidateQueries({ queryKey: ["roles"] });

      toast.success("Rol creado correctamente");

      return response;
    },
    [client],
  );

  return (
    <RolForm initialForm={initialData} type="create" onSubmit={onSubmit} />
  );
}
