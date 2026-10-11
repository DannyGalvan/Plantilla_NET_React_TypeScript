import { useState, type ReactNode } from 'react';

export interface TooltipProps {
  label: ReactNode;
  children: ReactNode;
}

/**
 * CSS-only tooltip: the bubble shows on hover or keyboard focus. Avoids
 * floating-portal libraries while preserving keyboard a11y.
 */
export function Tooltip({ label, children }: TooltipProps) {
  const [open, setOpen] = useState(false);
  return (
    <span
      className="tooltip"
      onBlur={() => setOpen(false)}
      onFocus={() => setOpen(true)}
      onMouseEnter={() => setOpen(true)}
      onMouseLeave={() => setOpen(false)}
    >
      <button className="tooltip-trigger" tabIndex={0} type="button">
        {children}
      </button>
      {open && (
        <span className="tooltip-bubble" role="tooltip" style={{ opacity: 1 }}>
          {label}
        </span>
      )}
    </span>
  );
}
