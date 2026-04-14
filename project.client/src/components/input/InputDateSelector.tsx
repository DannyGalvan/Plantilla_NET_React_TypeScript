import type { DateValue, RangeValue } from "@heroui/calendar";
import { DateRangePicker } from "@heroui/date-picker";
import { I18nProvider } from "@react-aria/i18n";
import { useCallback } from "react";
import { useRangeOfDatesStore } from "../../stores/useRangeOfDatesStore";

interface InputDateSelectorProps {
  readonly label: string;
}

export function InputDateSelector({ label }: InputDateSelectorProps) {
  const { setRageOfDates, calendarDate, setCalendarDate } =
    useRangeOfDatesStore();

  const handleChange = useCallback(
    (date: RangeValue<DateValue> | null) => {
      if (date) {
        setRageOfDates({
          start: date.start.toString(),
          end: date.end.toString(),
        });
        setCalendarDate(date);
      } else {
        setRageOfDates({ start: "", end: "" });
        setCalendarDate(null);
      }
    },
    [setRageOfDates, setCalendarDate],
  );

  return (
    <I18nProvider locale="es-Ca">
      <DateRangePicker
        showMonthAndYearPickers
        aria-label="Seleccionar rango de fechas"
        className="max-w-xs my-2"
        defaultValue={calendarDate as unknown as RangeValue<DateValue>}
        label={label}
        onChange={handleChange}
      />
    </I18nProvider>
  );
}
