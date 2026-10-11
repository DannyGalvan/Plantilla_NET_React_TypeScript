import { useEffect, useState } from 'react';

import { Button } from '../../../shared/ui/button';
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from '../../../shared/ui/card';
import {
  listProducts,
  type PagedResult,
  type ProductDto,
} from '../api/catalog-api';

export function ProductsPage() {
  const [page, setPage] = useState<PagedResult<ProductDto> | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    listProducts()
      .then(setPage)
      .catch((err: unknown) => setError((err as Error).message));
  }, []);

  return (
    <main style={{ padding: '2rem', maxWidth: '80rem', margin: '0 auto' }}>
      <Card>
        <CardHeader
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
          }}
        >
          <CardTitle>Productos</CardTitle>
          <Button size="sm">Nuevo producto</Button>
        </CardHeader>
        <CardContent>
          {error && <p style={{ color: 'var(--destructive)' }}>{error}</p>}
          {page ? (
            <table style={{ width: '100%', borderCollapse: 'collapse' }}>
              <thead>
                <tr>
                  <th style={{ textAlign: 'left', padding: '0.5rem' }}>SKU</th>
                  <th style={{ textAlign: 'left', padding: '0.5rem' }}>
                    Nombre
                  </th>
                  <th style={{ textAlign: 'right', padding: '0.5rem' }}>
                    Precio
                  </th>
                  <th style={{ textAlign: 'right', padding: '0.5rem' }}>
                    Stock
                  </th>
                </tr>
              </thead>
              <tbody>
                {page.items.map((p) => (
                  <tr
                    key={p.id}
                    style={{ borderTop: '1px solid var(--border)' }}
                  >
                    <td style={{ padding: '0.5rem' }}>{p.sku}</td>
                    <td style={{ padding: '0.5rem' }}>{p.name}</td>
                    <td style={{ padding: '0.5rem', textAlign: 'right' }}>
                      {p.price}
                    </td>
                    <td style={{ padding: '0.5rem', textAlign: 'right' }}>
                      {p.stock}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          ) : (
            <p>Cargando…</p>
          )}
        </CardContent>
      </Card>
    </main>
  );
}
