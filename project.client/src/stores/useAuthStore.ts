import { create } from "zustand";

import { setAuthorization } from "../configs/axios/interceptors";
import { authInitialState } from "../configs/constants";
import type { InitialAuth } from "../types/InitialAuth";
import { retrase } from "../utils/viewTransition";

interface AuthState {
  authState: InitialAuth;
  loading: boolean;
  syncAuth: () => void;
  signIn: (login: InitialAuth) => void;
  logout: () => void;
}

export const useAuthStore = create<AuthState>((set, get) => ({
  authState: authInitialState,
  loading: true,
  syncAuth: async () => {
    try {
      set({ loading: true });
      await retrase(1000);
      const storedState = window.localStorage.getItem("@auth");
      if (storedState) {
        const initialState: InitialAuth = JSON.parse(storedState);
        setAuthorization(initialState.token);
        set({ authState: initialState });
      }
      set({ loading: false });
    } catch (error: unknown) {
      console.error("Error during auth sync:", error);
      set({ loading: false });
    }
  },
  signIn: (auth) => {
    const newState = {
      ...get().authState,
      ...auth,
    };
    set({ authState: newState });
    setAuthorization(auth.token);
    window.localStorage.setItem("@auth", JSON.stringify(newState));
    return newState;
  },
  logout: async () => {
    try {
      set({ loading: true });
      await retrase(1000);
      window.localStorage.clear();
      setAuthorization("");
      set({ authState: authInitialState });
      set({ loading: false });
      return authInitialState;
    } catch (ex: unknown) {
      console.error("Error during logout:", ex);
      set({ loading: false });
    }
  },
}));
