import { Button, Dropdown, toast } from "@heroui/react";
import { useQueryClient } from "@tanstack/react-query";
import { useCallback, useState } from "react";
import { useNavigate } from "react-router";
import { deleteRol } from "../../services/rolService";
import type { RolResponse } from "../../types/RolResponse";
import { validationFailureToString } from "../../utils/converted";
import { Icon } from "../icons/Icon";
import { ConfirmDialog } from "../modal/ConfirmDialog";

interface RolButtonProps {
  readonly data: RolResponse;
}

export function RolButton({ data }: RolButtonProps) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);

  const handleEdit = useCallback(() => {
    navigate(`/rol/update/${data.id}`);
  }, [navigate, data.id]);

  const handleDeleteClick = useCallback(() => {
    setIsDeleteDialogOpen(true);
  }, []);

  const handleDeleteConfirm = useCallback(async () => {
    setIsDeleting(true);

    try {
      const response = await deleteRol(data.id);

      if (response.success) {
        toast.success(`Rol "${data.name}" eliminado correctamente`);
        await queryClient.invalidateQueries({ queryKey: ["roles"] });
        setIsDeleteDialogOpen(false);
      } else {
        toast.danger(
          `No se pudo eliminar el rol: ${response.message} ${validationFailureToString(response.data)}`,
        );
      }
    } catch (error) {
      toast.danger(
        `Error al eliminar el rol: ${error instanceof Error ? error.message : "Error desconocido"}`,
      );
    } finally {
      setIsDeleting(false);
    }
  }, [data, queryClient]);

  const handleCloseDialog = useCallback(() => {
    if (!isDeleting) {
      setIsDeleteDialogOpen(false);
    }
  }, [isDeleting]);

  return (
    <>
      <Dropdown>
        <Dropdown.Trigger>
          <Button isIconOnly size="sm" variant="primary">
            <Icon name="bi bi-three-dots-vertical" />
          </Button>
        </Dropdown.Trigger>
        <Dropdown.Popover>
          <Dropdown.Menu aria-label="Action event">
            <Dropdown.Item
              key="edit"
              className="text-warning hover:text-white"
              onClick={handleEdit}
            >
              Editar
            </Dropdown.Item>
            <Dropdown.Item
              key="delete"
              className="text-danger hover:text-white"
              onClick={handleDeleteClick}
            >
              Eliminar
            </Dropdown.Item>
          </Dropdown.Menu>
        </Dropdown.Popover>
      </Dropdown>

      <ConfirmDialog
        cancelText="Cancelar"
        confirmText="Eliminar"
        isLoading={isDeleting}
        isOpen={isDeleteDialogOpen}
        message={`¿Está seguro que desea eliminar el rol "${data.name}"? Esta acción no se puede deshacer.`}
        title="Confirmar eliminación"
        onClose={handleCloseDialog}
        onConfirm={handleDeleteConfirm}
      />
    </>
  );
}
