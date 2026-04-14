import type { ReactNode } from "react";

interface ColProps {
  readonly children?: ReactNode;
  readonly xs?: number;
  readonly sm?: number;
  readonly md?: number;
  readonly lg?: number;
  readonly xl?: number;
  readonly className?: string;
}

export function Col({ children, xs, sm, md, lg, xl, className }: ColProps) {
  const colClasses = [
    xs ? `col-${xs}` : "col",
    sm ? `col-sm-${sm}` : "",
    md ? `col-md-${md}` : "",
    lg ? `col-lg-${lg}` : "",
    xl ? `col-xl-${xl}` : "",
    className,
    "px-2",
  ];

  return <div className={colClasses.join(" ")}>{children}</div>;
}
