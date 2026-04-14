import { create } from "zustand";
import type { ListFilter } from "../types/ListFilter";

interface OperationFilterState {
  filters: ListFilter;
  setFilters: (filters: ListFilter) => void;
}

export const useOperationStore = create<OperationFilterState>((set) => ({
  filters: { filter: "", page: 1, pageSize: 10 },
  setFilters: (filters) => set({ filters }),
}));
