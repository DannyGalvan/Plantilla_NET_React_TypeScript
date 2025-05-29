import { create } from "zustand";
import type { AppError } from "../types/AppError";

interface ErrorState {
  error: AppError | null;
  setError: (error: AppError | null) => void;
  resetError: () => void;
}

export const useErrorsStore = create<ErrorState>((set) => ({
  error: null,
  setError: (error) => set({ error }),
  resetError: () => set({ error: null }),
}));
