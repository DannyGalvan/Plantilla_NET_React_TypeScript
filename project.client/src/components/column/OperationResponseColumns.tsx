import type { OperationResponse } from "../../types/OperationResponse";
import type { TableColumnWithFilters } from "../../types/TableColumnWithFilters";
import { OperationButton } from "../button/OperationButton";

export const OperationResponseColumns: TableColumnWithFilters<OperationResponse>[] =
  [
    {
      id: "id",
      name: "ID",
      selector: (data) => data.id ?? "",
      sortable: true,
      wrap: true,
      omit: false,
      hasFilter: true,
      filterField: (value) => (value ? `Id:eq:${value}` : ""),
    },
    {
      id: "name",
      name: "Nombre",
      selector: (data) => data.name ?? "",
      sortable: true,
      wrap: true,
      omit: false,
      hasFilter: true,
      filterField: (value) => (value ? `Name:like:${value}` : ""),
    },
    {
      id: "description",
      name: "Descripción",
      selector: (data) => data.description ?? "",
      sortable: true,
      wrap: true,
      omit: false,
      hasFilter: true,
      filterField: (value) => (value ? `Description:like:${value}` : ""),
    },
    {
      id: "module",
      name: "Modulo",
      selector: (data) => data.module?.name ?? "",
      sortable: true,
      wrap: true,
      omit: false,
      hasFilter: true,
      filterField: (value) => (value ? `Module.Name:like:${value}` : ""),
    },
    {
      id: "actions",
      name: "Acciones",
      width: "160px",
      center: true,
      button: true,
      cell: (data) => <OperationButton data={data} />,
    },
    {
      id: "createdAt",
      name: "Creado",
      selector: (data) => data.createdAt ?? "",
      sortable: true,
      maxWidth: "160px",
      omit: true,
    },
    {
      id: "updatedAt",
      name: "Actualizado",
      selector: (data) => data.updatedAt ?? "",
      sortable: true,
      maxWidth: "160px",
      omit: true,
    },
  ];
