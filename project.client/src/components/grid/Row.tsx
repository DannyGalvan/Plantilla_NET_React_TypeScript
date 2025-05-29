import type { ReactNode } from "react";

interface RowProps {
  readonly children: ReactNode;
  readonly className?: string;
}

export function Row({ children, className }: RowProps) {
  return <div className={`flex flex-wrap ${className}`}>{children}</div>;
}
