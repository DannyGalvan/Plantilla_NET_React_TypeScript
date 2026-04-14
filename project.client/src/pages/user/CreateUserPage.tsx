import { toast } from "@heroui/react";
import { useQueryClient } from "@tanstack/react-query";
import { useCallback } from "react";
import { UserForm } from "../../components/form/UserForm";
import { initialUser } from "../../configs/constants";
import { createUser } from "../../services/userService";
import type { UserRequest } from "../../types/UserRequest";

export function CreateUserPage() {
  const client = useQueryClient();

  const onSubmit = useCallback(
    async (form: UserRequest) => {
      const response = await createUser(form);

      if (!response.success) {
        toast.danger(response.message);
        return response;
      }

      await client.invalidateQueries({ queryKey: ["users"] });

      toast.success("Usuario creado correctamente");

      return response;
    },
    [client],
  );

  return (
    <UserForm initialForm={initialUser} type="create" onSubmit={onSubmit} />
  );
}
