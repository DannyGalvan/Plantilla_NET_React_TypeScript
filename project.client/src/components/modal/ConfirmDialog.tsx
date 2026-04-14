import { Button, Modal } from "@heroui/react";
import { Icon } from "../icons/Icon";

interface ConfirmDialogProps {
  readonly isOpen: boolean;
  readonly onClose: () => void;
  readonly onConfirm: () => void;
  readonly title: string;
  readonly message: string;
  readonly confirmText?: string;
  readonly cancelText?: string;
  readonly isLoading?: boolean;
}

export function ConfirmDialog({
  isOpen,
  onClose,
  onConfirm,
  title,
  message,
  confirmText = "Confirmar",
  cancelText = "Cancelar",
  isLoading = false,
}: ConfirmDialogProps) {
  return (
    <Modal isOpen={isOpen} onOpenChange={onClose}>
      <Modal.Backdrop>
        <Modal.Container>
          <Modal.Dialog className="max-w-md w-full">
            <Modal.CloseTrigger />
            <Modal.Header>
              <Modal.Heading>{title}</Modal.Heading>
            </Modal.Header>
            <Modal.Body>
              <div className="flex items-start gap-4 p-4">
                <div className="flex-shrink-0">
                  <Icon
                    color="rgb(239, 68, 68)"
                    name="bi bi-exclamation-triangle"
                    size={30}
                  />
                </div>
                <p className="text-sm text-gray-700 dark:text-gray-300">
                  {message}
                </p>
              </div>
            </Modal.Body>
            <Modal.Footer>
              <div className="flex gap-2 justify-end w-full">
                <Button
                  isDisabled={isLoading}
                  variant="secondary"
                  onPress={onClose}
                >
                  {cancelText}
                </Button>
                <Button
                  isPending={isLoading}
                  variant="danger"
                  onPress={onConfirm}
                >
                  {confirmText}
                </Button>
              </div>
            </Modal.Footer>
          </Modal.Dialog>
        </Modal.Container>
      </Modal.Backdrop>
    </Modal>
  );
}
