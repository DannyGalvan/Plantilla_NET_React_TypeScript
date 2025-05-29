import type { ReactNode } from "react";

interface LayoutLoginProps {
  readonly children: ReactNode;
}

export function LayoutLogin({ children }: LayoutLoginProps) {
  return <main className="flex h-[100vh] justify-center">{children}</main>;
}
