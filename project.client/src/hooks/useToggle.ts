import { useState } from "react";

export const useToggle = () => {
  const [open, setOpen] = useState<boolean>(false);

  const toggle = () => {
    setOpen(!open);
  };

  return {
    open,
    toggle,
  };
};
