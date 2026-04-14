import { Modal, ModalBody, ModalContent, ModalHeader } from "@heroui/modal";
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
    <Modal isOpen={open} size="3xl" onClose={toggle}>
      <ModalContent>
        <ModalHeader>Campos Visibles</ModalHeader>
        <ModalBody>
          <div className="grid grid-cols-2 gap-4 p-6">
            {columns.map((column) => (
              <ColumnItem
                key={column.id}
                changeVisibilitiColumn={changeVisibilitiColumn}
                column={column}
              />
            ))}
          </div>
        </ModalBody>
      </ModalContent>
    </Modal>
  );
}
