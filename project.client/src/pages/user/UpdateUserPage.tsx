import { toast } from "@heroui/react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useCallback } from "react";
import { useParams } from "react-router";
import { UserForm } from "../../components/form/UserForm";
import { LoadingComponent } from "../../components/spinner/LoadingComponent";
import { getUserById, updateUser } from "../../services/userService";
import type { UserRequest } from "../../types/UserRequest";
import { validationFailureToString } from "../../utils/converted";

export function UpdateUserPage() {
  const { id } = useParams();
  const client = useQueryClient();

  const {
    data: userToUpdate,
    isLoading,
    error,
  } = useQuery({
    queryKey: ["userToUpdate", id],
    queryFn: () => getUserById(Number(id)),
  });

  const onSubmit = useCallback(
    async (form: UserRequest) => {
      form.createdBy = null;
      const response = await updateUser(form);

      if (!response.success) {
        toast.danger(
          `${response.message} ${validationFailureToString(response.data)}`,
        );
        return response;
      }

      await client.invalidateQueries({ queryKey: ["users"] });
      await client.invalidateQueries({ queryKey: ["userToUpdate", id] });

      toast.success("Usuario actualizado correctamente");

      return response;
    },
    [client, id],
  );

  if (isLoading) {
    return <LoadingComponent />;
  }

  return (
    <div>
      {userToUpdate?.success ? (
        <UserForm
          initialForm={userToUpdate?.data}
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
