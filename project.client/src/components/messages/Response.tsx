import { useEffect, useState } from "react";

interface ResponseProps {
  readonly message: string;
  readonly type: boolean;
  readonly complement?: string;
}

export function Response({ message, type, complement }: ResponseProps) {
  const [visible, setVisible] = useState(true);

  useEffect(() => {
    const timer = setTimeout(() => {
      setVisible(false);
    }, 3000);

    return () => clearTimeout(timer);
  }, []);

  return (
    <div
      className={`my-1 flex w-full flex-row rounded-md ${
        type ? "bg-emerald-500" : "bg-rose-500"
      } p-2 shadow-lg transition-opacity duration-1000 ${
        !visible ? "opacity-0" : "opacity-100"
      }`}
    >
      <div className="ml-1">
        <p className="font-bold text-white">
          {message} {complement}
        </p>
      </div>
    </div>
  );
}
