import { create } from "zustand";
import type { ListFilter } from "../types/ListFilter";

interface RolFilterState {
  filters: ListFilter;
  setFilters: (filters: ListFilter) => void;
}

export const useRolStore = create<RolFilterState>((set) => ({
  filters: { filter: "", page: 1, pageSize: 10 },
  setFilters: (filters) => set({ filters }),
}));
