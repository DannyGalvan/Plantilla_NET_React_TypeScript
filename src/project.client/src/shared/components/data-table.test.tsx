import { render, screen, within } from '@testing-library/react';
import { userEvent } from '@testing-library/user-event';
import type { ColumnDef } from '@tanstack/react-table';
import { createMemoryRouter, RouterProvider, useLocation } from 'react-router';
import { describe, expect, it } from 'vitest';

import { DataTable, toListParams, useTableState } from './data-table';

interface Row {
  id: string;
  name: string;
}

const columns: ColumnDef<Row, unknown>[] = [{ accessorKey: 'name', header: 'Name', meta: { sortField: 'name' } }];

function Harness() {
  const [state, setState] = useTableState({ sort: 'name' });
  const location = useLocation();
  return (
    <>
      <output data-testid="location">{location.search}</output>
      <DataTable
        id="test"
        columns={columns}
        isLoading={false}
        state={state}
        onStateChange={setState}
        data={{
          items: [
            { id: '1', name: 'Laptop' },
            { id: '2', name: 'Mouse' },
          ],
          page: state.page,
          pageSize: state.pageSize,
          total: 45,
          totalPages: 3,
        }}
      />
    </>
  );
}

describe('DataTable', () => {
  it('builds list parameters with search and extra filters', () => {
    expect(
      toListParams({ page: 2, pageSize: 50, sort: '-price', q: 'lap' }, (q) => `name:contains:${q}`, 'categoryId:eq:1'),
    ).toEqual({
      page: 2,
      pageSize: 50,
      sort: '-price',
      filter: 'name:contains:lap;categoryId:eq:1',
    });
    expect(toListParams({ page: 1, pageSize: 20 }).filter).toBeUndefined();
  });

  it('renders rows and keeps paging and sorting in the URL', async () => {
    const router = createMemoryRouter([{ path: '/', element: <Harness /> }], { initialEntries: ['/'] });
    render(<RouterProvider router={router} />);

    const table = screen.getByRole('table');
    expect(within(table).getByText('Laptop')).toBeInTheDocument();
    expect(screen.getByText('45 records')).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'Next' }));
    expect(screen.getByTestId('location')).toHaveTextContent('page=2');

    await userEvent.click(within(table).getByRole('button', { name: /Name/ }));
    expect(screen.getByTestId('location')).toHaveTextContent('sort=-name');
    expect(screen.getByTestId('location')).toHaveTextContent('page=1');
  });
});
