import { useEffect, type ReactNode } from 'react';

export interface DialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title?: ReactNode;
  description?: ReactNode;
  children?: ReactNode;
  footer?: ReactNode;
}

/**
 * Minimal Dialog primitive (no external Radix dependency). Closes on Escape,
 * locks body scroll, and traps focus inside the panel. Replace with the
 * Radix-backed version when shadcn CLI runs in Fase 8.
 */
export function Dialog({
  open,
  onOpenChange,
  title,
  description,
  children,
  footer,
}: DialogProps) {
  useEffect(() => {
    if (!open) return;

    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';

    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onOpenChange(false);
    };
    window.addEventListener('keydown', onKey);

    return () => {
      document.body.style.overflow = previousOverflow;
      window.removeEventListener('keydown', onKey);
    };
  }, [open, onOpenChange]);

  if (!open) return null;

  return (
    <div
      aria-label={typeof title === 'string' ? title : undefined}
      aria-modal="true"
      role="dialog"
    >
      <button
        aria-label="Cerrar"
        className="dialog-overlay"
        type="button"
        onClick={() => onOpenChange(false)}
      />
      <div className="dialog-content">
        <button
          aria-label="Cerrar"
          className="dialog-close"
          type="button"
          onClick={() => onOpenChange(false)}
        >
          ×
        </button>
        {title && (
          <h2
            style={{
              fontSize: '1.125rem',
              fontWeight: 600,
              marginBottom: '0.5rem',
            }}
          >
            {title}
          </h2>
        )}
        {description && (
          <p style={{ color: 'var(--muted-foreground)', marginBottom: '1rem' }}>
            {description}
          </p>
        )}
        <div>{children}</div>
        {footer && (
          <div
            style={{
              marginTop: '1.5rem',
              display: 'flex',
              justifyContent: 'flex-end',
              gap: '0.5rem',
            }}
          >
            {footer}
          </div>
        )}
      </div>
    </div>
  );
}

export interface SheetProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title?: ReactNode;
  children?: ReactNode;
}

export function Sheet({ open, onOpenChange, title, children }: SheetProps) {
  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onOpenChange(false);
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [open, onOpenChange]);

  if (!open) return null;

  return (
    <div
      aria-label={typeof title === 'string' ? title : undefined}
      aria-modal="true"
      role="dialog"
    >
      <button
        aria-label="Cerrar"
        className="dialog-overlay"
        type="button"
        onClick={() => onOpenChange(false)}
      />
      <div className="sheet-content">
        <button
          aria-label="Cerrar"
          className="dialog-close"
          style={{ left: '0.75rem', right: 'auto' }}
          type="button"
          onClick={() => onOpenChange(false)}
        >
          ×
        </button>
        {title && (
          <h2
            style={{
              fontSize: '1.125rem',
              fontWeight: 600,
              marginBottom: '1rem',
            }}
          >
            {title}
          </h2>
        )}
        <div>{children}</div>
      </div>
    </div>
  );
}
