import type { ReactNode } from 'react';
import type { FieldValues, Path, UseFormReturn } from 'react-hook-form';
import { toast } from 'sonner';

import { ApiError } from '@/shared/api/problem-details';
import { Label } from '@/shared/ui';

/** Label + control + error message, wired for accessibility (aria-invalid, aria-describedby). */
export function FormField({
  id,
  label,
  error,
  hint,
  children,
}: {
  id: string;
  label: string;
  error?: string;
  hint?: string;
  children: ReactNode;
}) {
  return (
    <div className="grid gap-2">
      <Label htmlFor={id}>{label}</Label>
      {children}
      {hint && !error && <p className="text-xs text-muted-foreground">{hint}</p>}
      {error && (
        <p id={`${id}-error`} role="alert" className="text-sm text-destructive">
          {error}
        </p>
      )}
    </div>
  );
}

/** Accessibility props for a control rendered inside <FormField>. */
export function fieldProps(id: string, error?: string) {
  return { id, 'aria-invalid': error ? true : undefined, 'aria-describedby': error ? `${id}-error` : undefined };
}

/**
 * Shows server-side validation errors on the matching fields (ProblemDetails `errors`) and a toast for anything
 * that is not a field error. Returns true when the error was handled.
 */
export function applyServerErrors<T extends FieldValues>(form: UseFormReturn<T>, error: unknown): void {
  const apiError = ApiError.from(error);
  const fields = Object.keys(form.getValues());
  let assigned = false;

  for (const [field, messages] of Object.entries(apiError.errors)) {
    const name = fields.find((key) => key.toLowerCase() === field.toLowerCase());
    if (name && messages[0]) {
      form.setError(name as Path<T>, { type: 'server', message: messages[0] });
      assigned = true;
    }
  }

  if (!assigned) {
    toast.error(apiError.message, { description: apiError.traceId ? `traceId: ${apiError.traceId}` : undefined });
  }
}
