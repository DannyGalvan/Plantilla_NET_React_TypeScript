import type { DateValue, RangeValue } from "@heroui/react";
import {
  DateField,
  DateRangePicker,
  Label,
  RangeCalendar,
} from "@heroui/react";
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
        className="max-w-xs my-2 flex flex-col gap-1"
        defaultValue={calendarDate as unknown as RangeValue<DateValue>}
        onChange={handleChange}
      >
        <Label className="text-sm font-medium">{label}</Label>
        <DateField.Group className="flex rounded-md border border-gray-300 dark:border-zinc-700 bg-white dark:bg-zinc-900 px-3 py-2 items-center">
          <DateField.InputContainer className="flex items-center">
            <DateField.Input slot="start">
              {(segment) => <DateField.Segment segment={segment} />}
            </DateField.Input>
            <DateRangePicker.RangeSeparator className="px-2 text-gray-500">
              -
            </DateRangePicker.RangeSeparator>
            <DateField.Input slot="end">
              {(segment) => <DateField.Segment segment={segment} />}
            </DateField.Input>
          </DateField.InputContainer>
          <DateField.Suffix className="ml-auto">
            <DateRangePicker.Trigger
              aria-label="Seleccionar rango de fechas"
              className="flex items-center justify-center outline-none"
            >
              <DateRangePicker.TriggerIndicator className="text-gray-500" />
            </DateRangePicker.Trigger>
          </DateField.Suffix>
        </DateField.Group>
        <DateRangePicker.Popover className="rounded-xl border border-gray-200 bg-white p-2 shadow-xl dark:border-zinc-700 dark:bg-zinc-900 z-50">
          <RangeCalendar aria-label="Calendario">
            <RangeCalendar.Header className="flex w-full items-center justify-between pb-2">
              <RangeCalendar.YearPickerTrigger className="outline-none rounded hover:bg-gray-100 dark:hover:bg-zinc-800 px-2 py-1 transition-colors">
                <RangeCalendar.YearPickerTriggerHeading className="font-semibold text-sm" />
                <RangeCalendar.YearPickerTriggerIndicator />
              </RangeCalendar.YearPickerTrigger>
              <div className="flex gap-1">
                <RangeCalendar.NavButton
                  className="p-1 rounded hover:bg-gray-100 dark:hover:bg-zinc-800"
                  slot="previous"
                />
                <RangeCalendar.NavButton
                  className="p-1 rounded hover:bg-gray-100 dark:hover:bg-zinc-800"
                  slot="next"
                />
              </div>
            </RangeCalendar.Header>
            <RangeCalendar.Grid className="w-full border-collapse">
              <RangeCalendar.GridHeader>
                {(day) => (
                  <RangeCalendar.HeaderCell className="text-xs font-medium text-gray-500 pb-1">
                    {day}
                  </RangeCalendar.HeaderCell>
                )}
              </RangeCalendar.GridHeader>
              <RangeCalendar.GridBody>
                {(date) => (
                  <RangeCalendar.Cell
                    className="w-8 h-8 rounded-md hover:bg-blue-50 dark:hover:bg-blue-900 cursor-pointer flex items-center justify-center text-sm"
                    date={date}
                  />
                )}
              </RangeCalendar.GridBody>
            </RangeCalendar.Grid>
          </RangeCalendar>
        </DateRangePicker.Popover>
      </DateRangePicker>
    </I18nProvider>
  );
}
