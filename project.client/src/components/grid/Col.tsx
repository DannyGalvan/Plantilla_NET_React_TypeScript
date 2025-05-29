import type { ReactNode } from "react";

interface ColProps {
  children?: ReactNode;
  xs?: number;
  sm?: number;
  md?: number;
  lg?: number;
  xl?: number;
  className?: string;
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
