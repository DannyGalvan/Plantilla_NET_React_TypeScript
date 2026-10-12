/** Locale-aware formatting with Intl. Locale, currency and time zone come from the cascading settings. */
export interface FormatSettings {
  locale: string;
  currency: string;
  timeZone: string;
}

let current: FormatSettings = { locale: 'es', currency: 'USD', timeZone: 'UTC' };

export function setFormatSettings(settings: Partial<FormatSettings>) {
  current = { ...current, ...settings };
}

export function getFormatSettings(): FormatSettings {
  return current;
}

export function formatCurrency(value: number, currency = current.currency) {
  return new Intl.NumberFormat(current.locale, { style: 'currency', currency }).format(value);
}

export function formatNumber(value: number, options?: Intl.NumberFormatOptions) {
  return new Intl.NumberFormat(current.locale, options).format(value);
}

export function formatDate(
  value: string | Date | null | undefined,
  options: Intl.DateTimeFormatOptions = { dateStyle: 'medium' },
) {
  if (!value) {
    return '';
  }

  const date = typeof value === 'string' ? new Date(value) : value;
  return new Intl.DateTimeFormat(current.locale, { timeZone: current.timeZone, ...options }).format(date);
}

export function formatDateTime(value: string | Date | null | undefined) {
  return formatDate(value, { dateStyle: 'medium', timeStyle: 'short' });
}
