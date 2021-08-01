export const calendarService = {
    Google: 0
  } as const;
export type CalendarServiceType = typeof calendarService[keyof typeof calendarService];