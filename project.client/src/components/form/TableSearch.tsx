import type { Key } from "@heroui/react";
import {
  Button,
  Input,
  Label,
  ListBox,
  Select,
  TextField,
} from "@heroui/react";
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

  const handleChange = useCallback(
    (value: Key | null) => {
      if (value) {
        selectedField({
          target: { value: value as string },
        } as React.ChangeEvent<HTMLSelectElement>);
      }
    },
    [selectedField],
  );

  return (
    <Row className="mt-4">
      <Col md={6} sm={12}>
        <Select
          className="py-4 w-full"
          placeholder="Filtrar por campo"
          onChange={handleChange}
        >
          <Label className="sr-only">Filtrar por campo</Label>
          <Select.Trigger>
            <Select.Value />
            <Select.Indicator />
          </Select.Trigger>
          <Select.Popover>
            <ListBox>
              {columns
                .filter((x) => x.hasFilter)
                .map((item) => (
                  <ListBox.Item
                    key={item.id}
                    id={item.id}
                    textValue={item.name as string}
                  >
                    {item.name}
                  </ListBox.Item>
                ))}
            </ListBox>
          </Select.Popover>
        </Select>
      </Col>
      <Col md={6} sm={12}>
        <article className="flex justify-end items-center gap-2">
          <TextField className="py-4" name="search" onKeyDown={handleOnKeyDown}>
            <Label className="sr-only">Buscar...</Label>
            <Input ref={searchField} placeholder="Buscar..." type="search" />
          </TextField>
          <Button
            className="p-6"
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
