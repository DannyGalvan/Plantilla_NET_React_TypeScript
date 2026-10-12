import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';

import { useQueryScope } from '@/shared/api/query-keys';
import { EmptyState, PageHeader, QueryError, Spinner } from '@/shared/components/page';
import { formatCurrency, formatNumber } from '@/shared/lib/format';
import {
  Badge,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Input,
  Label,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/shared/ui';

import { reportsApi } from '../api';

export function StockReportPage() {
  const { t } = useTranslation('catalog');
  const scope = useQueryScope('catalog', 'reports');
  const [threshold, setThreshold] = useState(5);
  const stock = useQuery({ queryKey: [...scope, 'stock'], queryFn: reportsApi.stock });
  const lowStock = useQuery({
    queryKey: [...scope, 'low-stock', threshold],
    queryFn: () => reportsApi.lowStock(threshold),
  });

  const maxValue = Math.max(1, ...(stock.data ?? []).map((row) => row.inventoryValue));

  return (
    <div className="space-y-6">
      <PageHeader title={t('reports.title')} description={t('reports.description')} />

      <Card>
        <CardHeader>
          <CardTitle>{t('reports.value')}</CardTitle>
        </CardHeader>
        <CardContent>
          {stock.isLoading ? (
            <Spinner />
          ) : stock.error ? (
            <QueryError error={stock.error} onRetry={() => void stock.refetch()} />
          ) : (
            <div className="space-y-6">
              <div className="space-y-3" aria-hidden>
                {stock.data?.map((row) => (
                  <div key={row.categoryId} className="grid grid-cols-[8rem_1fr_7rem] items-center gap-3 text-sm">
                    <span className="truncate">{row.categoryName}</span>
                    <div className="h-3 rounded-full bg-muted">
                      <div
                        className="h-3 rounded-full bg-chart-1"
                        style={{ width: `${(row.inventoryValue / maxValue) * 100}%` }}
                      />
                    </div>
                    <span className="text-right tabular-nums">{formatCurrency(row.inventoryValue)}</span>
                  </div>
                ))}
              </div>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>{t('reports.category')}</TableHead>
                    <TableHead className="text-right">{t('reports.products')}</TableHead>
                    <TableHead className="text-right">{t('reports.units')}</TableHead>
                    <TableHead className="text-right">{t('reports.value')}</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {stock.data?.map((row) => (
                    <TableRow key={row.categoryId}>
                      <TableCell>{row.categoryName}</TableCell>
                      <TableCell className="text-right tabular-nums">{formatNumber(row.products)}</TableCell>
                      <TableCell className="text-right tabular-nums">{formatNumber(row.unitsInStock)}</TableCell>
                      <TableCell className="text-right tabular-nums">{formatCurrency(row.inventoryValue)}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader className="flex flex-row items-end justify-between gap-4">
          <div className="space-y-1.5">
            <CardTitle>{t('reports.lowStock')}</CardTitle>
            <CardDescription>GetLowStockProducts</CardDescription>
          </div>
          <div className="grid gap-1">
            <Label htmlFor="threshold">{t('reports.threshold')}</Label>
            <Input
              id="threshold"
              type="number"
              min={0}
              className="w-24"
              value={threshold}
              onChange={(event) => setThreshold(Math.max(0, Number(event.target.value)))}
            />
          </div>
        </CardHeader>
        <CardContent>
          {lowStock.error ? (
            <QueryError error={lowStock.error} onRetry={() => void lowStock.refetch()} />
          ) : lowStock.data?.length === 0 ? (
            <EmptyState title={t('reports.noLowStock')} />
          ) : (
            <ul className="divide-y">
              {lowStock.data?.map((row) => (
                <li key={row.id} className="flex items-center justify-between py-2 text-sm">
                  <span>
                    <span className="font-mono text-muted-foreground">{row.sku}</span> {row.name}
                  </span>
                  <Badge variant="warning">{formatNumber(row.stock)}</Badge>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
