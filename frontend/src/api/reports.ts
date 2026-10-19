import { apiRequest, toQueryString, type DownloadedFile } from './client';
import type { AnalyticsQuery } from './types';

/** ReportController - downloadable reports. */
export type ReportFormat = 'Pdf' | 'Csv';

/** Language of the report and the user's time zone (for the "generated at" time). */
export interface ReportOptions {
  format: ReportFormat;
  /** 'sr-Cyrl', 'sr-Latn' or 'en' - the app's current language. */
  lang: string;
  timeZone?: string;
}

/** The browser's time zone, e.g. "Europe/Sarajevo"; undefined if it cannot be read. */
export function browserTimeZone(): string | undefined {
  try {
    return Intl.DateTimeFormat().resolvedOptions().timeZone || undefined;
  } catch {
    return undefined;
  }
}

export const reportsApi = {
  /** Personal spending for the same period as the analytics page. */
  spending: (query: AnalyticsQuery, options: ReportOptions, signal?: AbortSignal) =>
    apiRequest<DownloadedFile>(`/report/spending${toQueryString({ ...query, ...options })}`, {
      responseType: 'file',
      signal,
    }),

  /** Balances, how to settle them, completed settlements and all expenses of a group. */
  group: (groupId: string, options: ReportOptions, signal?: AbortSignal) =>
    apiRequest<DownloadedFile>(`/report/groups/${groupId}${toQueryString({ ...options })}`, {
      responseType: 'file',
      signal,
    }),
};

/** Hands a downloaded file to the browser, which saves it like any other download. */
export function saveFile(file: DownloadedFile, fallbackName: string) {
  const url = URL.createObjectURL(file.blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = file.fileName ?? fallbackName;
  document.body.append(link);
  link.click();
  link.remove();
  // Some browsers start the download asynchronously; the address is released a bit later.
  window.setTimeout(() => URL.revokeObjectURL(url), 10_000);
}
