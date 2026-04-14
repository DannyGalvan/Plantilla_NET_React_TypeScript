import { Button, Dropdown } from "@heroui/react";

import { useCallback } from "react";
import { useNavigate } from "react-router";
import type { UserResponse } from "../../types/UserResponse";
import { Icon } from "../icons/Icon";

interface UserButtonProps {
  readonly data: UserResponse;
}

export function UserButton({ data }: UserButtonProps) {
  const navigate = useNavigate();

  const handleEdit = useCallback(() => {
    navigate(`/user/update/${data.id}`);
  }, [navigate, data.id]);

  return (
    <Dropdown>
      <Dropdown.Trigger>
        <Button isIconOnly size="sm" variant="primary">
          <Icon name="bi bi-three-dots-vertical" />
        </Button>
      </Dropdown.Trigger>
      <Dropdown.Menu aria-label="Action event">
        <Dropdown.Item
          key="viewDetails"
          className="text-warning hover:text-white"
          onClick={handleEdit}
        >
          Editar
        </Dropdown.Item>
      </Dropdown.Menu>
    </Dropdown>
  );
}
