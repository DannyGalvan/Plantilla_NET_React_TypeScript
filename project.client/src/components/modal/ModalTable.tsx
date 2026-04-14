import { Modal } from "@heroui/react";
import type { TableColumn } from "react-data-table-component";
import { ColumnItem } from "../pure/ColumnItem";

interface ModalTableProps<T> {
  readonly columns: TableColumn<T>[];
  readonly changeVisibilitiColumn: (column: TableColumn<T>) => void;
  readonly open: boolean;
  readonly toggle: () => void;
}

export function ModalTable<T>({
  columns,
  changeVisibilitiColumn,
  open,
  toggle,
}: ModalTableProps<T>) {
  return (
    <Modal isOpen={open} onOpenChange={toggle}>
      <Modal.Backdrop>
        <Modal.Container>
          <Modal.Dialog className="max-w-3xl w-full">
            <Modal.CloseTrigger />
            <Modal.Header>
              <Modal.Heading>Campos Visibles</Modal.Heading>
            </Modal.Header>
            <Modal.Body>
              <div className="grid grid-cols-2 gap-4 p-6">
                {columns.map((column) => (
                  <ColumnItem
                    key={column.id}
                    changeVisibilitiColumn={changeVisibilitiColumn}
                    column={column}
                  />
                ))}
              </div>
            </Modal.Body>
          </Modal.Dialog>
        </Modal.Container>
      </Modal.Backdrop>
    </Modal>
  );
}
