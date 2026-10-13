import { PackageIcon, TagsIcon, TriangleAlertIcon } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { countOf, KpiCard } from '@/shared/components/kpi-card';

export function ProductsKpi() {
  const { t } = useTranslation();
  return (
    <KpiCard
      id="catalog.products"
      title={t('nav.products')}
      icon={PackageIcon}
      permission="catalog.products.read"
      to="/catalog/products"
      load={() => countOf('/catalog/products')}
    />
  );
}

export function CategoriesKpi() {
  const { t } = useTranslation();
  return (
    <KpiCard
      id="catalog.categories"
      title={t('nav.categories')}
      icon={TagsIcon}
      permission="catalog.categories.read"
      to="/catalog/categories"
      load={() => countOf('/catalog/categories')}
    />
  );
}

export function LowStockKpi() {
  const { t } = useTranslation('catalog');
  return (
    <KpiCard
      id="catalog.low-stock"
      title={t('reports.lowStock')}
      icon={TriangleAlertIcon}
      permission="catalog.products.read"
      to="/catalog/products?sort=stock"
      load={() => countOf('/catalog/products', 'stock:lt:5')}
    />
  );
}
