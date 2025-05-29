import { useAuthStore } from "../stores/useAuthStore";
import { toAllOperations } from "../utils/converted";

export const useAuth = () => {
  const { authState, loading, logout, signIn, syncAuth } = useAuthStore();

  const {
    email,
    isLoggedIn,
    operations,
    token,
    userName,
    name,
    redirect,
    userId,
  } = authState;

  const allOperations = toAllOperations(operations);

  return {
    email,
    isLoggedIn,
    logout,
    signIn,
    token,
    userName,
    name,
    redirect,
    operations,
    allOperations,
    userId,
    loading,
    syncAuth,
  };
};
