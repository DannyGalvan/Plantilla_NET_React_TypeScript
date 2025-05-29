import type { HTMLAttributes } from "react";

interface IconProps {
  readonly name: HTMLAttributes<HTMLElement>["className"];
  readonly size?: number;
  readonly color?: string;
}

export function Icon({ name, size = 25, color = "white" }: IconProps) {
  const iconSize = `${size}px`;

  return <i className={name} style={{ fontSize: iconSize, color: color }} />;
}
