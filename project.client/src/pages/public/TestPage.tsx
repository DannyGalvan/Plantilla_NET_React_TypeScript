import { Button } from "@heroui/button";
import { useAuth } from "../../hooks/useAuth";

export function TestPage() {
  const { operations, logout } = useAuth();

  return (
    <div className="h-screen flex flex-col items-center justify-center bg-gray-100">
      <h1 className="font-bold text-2xl">Test Page</h1>
      <p>This is a test page to check the integration of Hero UI.</p>
      <p>Click the button below:</p>
      <Button color="primary" onPress={logout}>
        Sing Out
      </Button>
      <p>Operations: {JSON.stringify(operations, null, 5)}</p>
    </div>
  );
}
