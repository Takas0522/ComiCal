import { SafeHtml, SafeUrl } from "@angular/platform-browser";

export interface ComicInterface {
  isbn: string;
  title: string;
  titleKana: string;
  seriesName: string;
  seriesNameKana: string;
  author: string;
  authorKana: string;
  publisherName: string;
  salesDate: Date;
  scheduleStatus: ScheduleStatusType;
  imageBase64: string;
  imageBase64Sanitize: SafeUrl;
}

export const scheduleStatus = {
    Confirm: 0,
    UntilDay: 1,
    UntilMonth: 2,
    UntilYear: 3,
    Undecided: 9
  } as const;
export type ScheduleStatusType = typeof scheduleStatus[keyof typeof scheduleStatus];