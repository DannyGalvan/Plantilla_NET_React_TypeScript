import { Button, Dropdown } from "@heroui/react";
import { useCallback } from "react";
import { useNavigate } from "react-router";
import type { RolResponse } from "../../types/RolResponse";
import { Icon } from "../icons/Icon";

interface RolButtonProps {
  readonly data: RolResponse;
}

export function RolButton({ data }: RolButtonProps) {
  const navigate = useNavigate();

  const handleEdit = useCallback(() => {
    navigate(`/rol/update/${data.id}`);
  }, [navigate, data.id]);

  return (
    <Dropdown>
      <Dropdown.Trigger>
        <Button isIconOnly size="sm" variant="primary">
          <Icon name="bi bi-three-dots-vertical" />
        </Button>
      </Dropdown.Trigger>
      <Dropdown.Popover>
        <Dropdown.Menu aria-label="Action event">
          <Dropdown.Item
            key="viewDetails"
            className="text-warning hover:text-white"
            onClick={handleEdit}
          >
            Editar
          </Dropdown.Item>
        </Dropdown.Menu>
      </Dropdown.Popover>
    </Dropdown>
  );
}
