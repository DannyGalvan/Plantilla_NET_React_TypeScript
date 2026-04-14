import { create } from "zustand";
import type { ListFilter } from "../types/ListFilter";

interface UserFilterState {
  filters: ListFilter;
  setFilters: (filters: ListFilter) => void;
}

export const useUserStore = create<UserFilterState>((set) => ({
  filters: { filter: "", page: 1, pageSize: 10 },
  setFilters: (filters) => set({ filters }),
}));
