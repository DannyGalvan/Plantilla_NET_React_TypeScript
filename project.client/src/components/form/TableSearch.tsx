import { Button } from "@heroui/button";
import { Input } from "@heroui/input";
import { Select, SelectItem } from "@heroui/select";
import { useCallback, type Ref } from "react";

import type { TableColumnWithFilters } from "../../types/TableColumnWithFilters";
import { Col } from "../grid/Col";
import { Row } from "../grid/Row";
import { Icon } from "../icons/Icon";

interface TableSearchProps<T> {
  readonly selectedField: (e: React.ChangeEvent<HTMLSelectElement>) => void;
  readonly columns: TableColumnWithFilters<T>[];
  readonly searchField: Ref<HTMLInputElement>;
  readonly filterData: () => void;
}

export function TableSearch<T extends object>({
  selectedField,
  columns,
  searchField,
  filterData,
}: TableSearchProps<T>) {
  const handleOnKeyDown = useCallback(
    (e: React.KeyboardEvent<HTMLInputElement>) => {
      if (e.key === "Enter" || e.key === "Tab") {
        e.preventDefault();
        filterData();
      }
    },
    [filterData],
  );

  const handleOnClick = useCallback(() => {
    filterData();
  }, [filterData]);

  return (
    <Row className="mt-4">
      <Col md={6} sm={12}>
        <Select
          aria-label="Filtrar por campo"
          className="py-4"
          label="Filtrar por campo"
          size="sm"
          variant="bordered"
          onChange={selectedField}
        >
          {columns
            .filter((x) => x.hasFilter)
            .map((item) => (
              <SelectItem key={item.id}>{item.name}</SelectItem>
            ))}
        </Select>
      </Col>
      <Col md={6} sm={12}>
        <article className="flex justify-end items-center gap-2">
          <Input
            ref={searchField}
            className="py-4"
            label="Buscar..."
            name="search"
            size="sm"
            type="search"
            variant="bordered"
            onKeyDown={handleOnKeyDown}
          />
          <Button
            className="p-6"
            color="primary"
            radius="sm"
            size="sm"
            type="button"
            onPress={handleOnClick}
          >
            <Icon name="bi bi-search" />
          </Button>
        </article>
      </Col>
    </Row>
  );
}
