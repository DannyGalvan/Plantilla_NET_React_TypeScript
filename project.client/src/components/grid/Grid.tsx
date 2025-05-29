import type { ReactNode } from "react";

interface GridProps {
  readonly children: ReactNode;
  readonly xs?: number;
  readonly sm?: number;
  readonly md?: number;
  readonly lg?: number;
  readonly xl?: number;
  readonly gap?: number;
  readonly className?: string;
}

export function Grid({
  children,
  xs,
  sm,
  md,
  lg,
  xl,
  gap,
  className,
}: GridProps) {
  const colClasses = [
    "grid",
    xs ? `gcol-${xs}` : "",
    sm ? `gcol-sm-${sm}` : "",
    md ? `gcol-md-${md}` : "",
    lg ? `gcol-lg-${lg}` : "",
    xl ? `gcol-xl-${xl}` : "",
    `gap-${gap ?? 4}`,
    "px-2",
    className,
  ];
  return <div className={colClasses.join(" ")}>{children}</div>;
}
