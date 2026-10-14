import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';

/**
 * Locale used for numbers and dates. Bosnian-Herzegovinian Serbian gives "1.234,50 КМ" and
 * "12. октобар 2026."; British English puts the day before the month, as people here expect.
 */
export function formattingLocale(language: string): string {
  if (language === 'sr-Cyrl') return 'sr-Cyrl-BA';
  if (language === 'sr-Latn') return 'sr-Latn-BA';
  return 'en-GB';
}

/** "2026-10-12T00:00:00Z" or "2026-10-12" -> "2026-10-12" */
export function toDateOnly(value: string): string {
  return value.slice(0, 10);
}

/** Today in the user's time zone as "YYYY-MM-DD" (what <input type="date"> uses). */
export function todayDateOnly(now = new Date()): string {
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  return `${now.getFullYear()}-${month}-${day}`;
}

/**
 * An expense date is a calendar day, not a moment. It is read as midnight UTC and formatted
 * in UTC, so the day never shifts by one for users west or east of Greenwich.
 */
function dateOnlyToUtc(value: string): Date {
  return new Date(`${toDateOnly(value)}T00:00:00Z`);
}

// Dates of this year are shown without the year. Read once, when the app loads.
const CURRENT_YEAR = new Date().getFullYear();

/** Formatting of money and dates in the selected language. */
export function useFormatters() {
  const { i18n } = useTranslation();
  const locale = formattingLocale(i18n.language);

  return useMemo(() => {
    const moneyFormats = new Map<string, Intl.NumberFormat>();
    const shortDate = new Intl.DateTimeFormat(locale, { day: 'numeric', month: 'short', timeZone: 'UTC' });
    const shortDateWithYear = new Intl.DateTimeFormat(locale, {
      day: 'numeric',
      month: 'short',
      year: 'numeric',
      timeZone: 'UTC',
    });
    const longDate = new Intl.DateTimeFormat(locale, { day: 'numeric', month: 'long', year: 'numeric', timeZone: 'UTC' });
    const monthFormat = new Intl.DateTimeFormat(locale, { month: 'long', year: 'numeric', timeZone: 'UTC' });

    return {
      locale,

      /** 1234.5, "BAM" -> "1.234,50 КМ" / "1.234,50 KM" / "KM 1,234.50" */
      money(amount: number, currency: string): string {
        let format = moneyFormats.get(currency);
        if (!format) {
          try {
            format = new Intl.NumberFormat(locale, {
              style: 'currency',
              currency,
              // English would write "BAM 12.00"; "KM 12.00" is what people here are used to.
              currencyDisplay: locale === 'en-GB' ? 'narrowSymbol' : 'symbol',
              // Always two decimals, also for RSD, so amounts line up in a column.
              minimumFractionDigits: 2,
              maximumFractionDigits: 2,
            });
          } catch {
            // Unknown currency code - plain number with the code after it.
            format = new Intl.NumberFormat(locale, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
            moneyFormats.set(currency, format);
            return `${format.format(amount)} ${currency}`;
          }
          moneyFormats.set(currency, format);
        }
        return format.format(amount);
      },

      /** "12. окт" this year, "12. окт 2025." for other years */
      shortDate(value: string): string {
        const date = dateOnlyToUtc(value);
        return (date.getUTCFullYear() === CURRENT_YEAR ? shortDate : shortDateWithYear).format(date);
      },

      /** "2026-10" -> "октобар 2026" / "October 2026" */
      month(value: string): string {
        // Serbian adds a period after the year ("октобар 2026."), which reads badly inside a sentence.
        return monthFormat.format(new Date(`${value}-01T00:00:00Z`)).replace(/\.$/, '');
      },

      /** "12. октобар 2026." */
      longDate(value: string): string {
        return longDate.format(dateOnlyToUtc(value));
      },
    };
  }, [locale]);
}

/** "YYYY-MM" of today in the user's time zone. */
export function currentMonth(now = new Date()): string {
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;
}

/** "2026-01", -1 -> "2025-12" */
export function addMonths(month: string, delta: number): string {
  const [year, monthNumber] = month.split('-').map(Number);
  const date = new Date(Date.UTC(year, monthNumber - 1 + delta, 1));
  return `${date.getUTCFullYear()}-${String(date.getUTCMonth() + 1).padStart(2, '0')}`;
}
