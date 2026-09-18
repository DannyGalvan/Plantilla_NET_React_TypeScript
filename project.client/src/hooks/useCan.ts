import { useAuth } from "./useAuth";

/**
 * F3: hook that returns whether the current user is allowed to perform
 * the given operation. Use it to gate buttons, menu items, and inline
 * actions — not just routes.
 *
 * Matching is by OperationKey (e.g. "User.Create.POST") which is the
 * canonical id used by the backend.
 */
export const useCan = (operationKey: string): boolean => {
  const { allOperations } = useAuth();
  if (!operationKey) return false;
  return (allOperations ?? []).some(
    (op) => (op.name ?? "").toString() === operationKey,
  );
};
