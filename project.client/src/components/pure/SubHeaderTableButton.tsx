import { Button, Tooltip } from "@heroui/react";
import { Icon } from "../icons/Icon";

interface SubHeaderTableButtonProps {
  readonly onClick: () => void;
}

export function SubHeaderTableButton({ onClick }: SubHeaderTableButtonProps) {
  return (
    <Tooltip closeDelay={0} delay={0}>
      <Button
        isIconOnly
        className="bg-transparent text-white rounded-sm"
        type="button"
        onClick={onClick}
      >
        <Icon name="bi bi-three-dots-vertical" />
      </Button>
      <Tooltip.Content className="px-2 py-1 bg-gray-900 text-white rounded-md text-sm">
        Campos Visibles
      </Tooltip.Content>
    </Tooltip>
  );
}
