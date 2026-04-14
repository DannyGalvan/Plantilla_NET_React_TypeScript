import type { DateValue, RangeValue } from "@heroui/calendar";
import { parseDate } from "@internationalized/date";
import { create } from "zustand";

import type { DateFilters } from "../types/DateFilters.ts";
import { minDateMaxDate } from "../utils/converted";

interface RangeOfDateState {
  calendarDate: RangeValue<DateValue> | null;
  setCalendarDate: (calendarDate: RangeValue<DateValue> | null) => void;
  start: string;
  end: string;
  setRageOfDates: ({ start, end }: DateFilters) => void;
  getDateFilters: (fieldName: string) => string;
  getCalendarDateFilter: (fieldName: string) => string;
  getCalendarDateTitle: () => string;
}

const dateRange = minDateMaxDate(1);

export const useRangeOfDatesStore = create<RangeOfDateState>((set, get) => ({
  start: dateRange.minDate,
  end: dateRange.maxDate,
  calendarDate: {
    start: parseDate(dateRange.minDate) as unknown as DateValue,
    end: parseDate(dateRange.maxDate) as unknown as DateValue,
  },
  setCalendarDate: (calendarDate) => {
    set({ calendarDate });
  },
  setRageOfDates: ({ start, end }) => {
    set({ start, end });
  },
  getDateFilters: (fieldName) => {
    const dateString = `${fieldName}:gt:${get().start}T00 AND ${fieldName}:lt:${get().end}T23`;
    return dateString;
  },
  getCalendarDateFilter: (fieldName) => {
    const calendarDate = get().calendarDate;
    const endAddOneDay = calendarDate?.end.add({ days: 1 });
    const dateString = `${fieldName}:gt:${calendarDate?.start.toString()}T00 AND ${fieldName}:lt:${endAddOneDay?.toString()}T06 AND State:eq:1`;
    return dateString;
  },
  getCalendarDateTitle: () => {
    const calendarDate = get().calendarDate;
    const endAddOneDay = calendarDate?.end.add({ days: 1 });
    return `Informe del ${calendarDate?.start.toString()} al ${endAddOneDay?.toString()}`;
  },
}));
