import { useTranslation } from 'react-i18next';

/** The currencies offered in the forms; the backend converts between them with daily rates. */
export const CURRENCIES = ['BAM', 'EUR', 'RSD'] as const;

/**
 * Options for a currency select: "КМ (BAM)", "€ (EUR)", "дин. (RSD)". A currency a record
 * already has stays selectable even if it is not one of the usual three.
 */
export function useCurrencyOptions(...extra: (string | null | undefined)[]) {
  const { t } = useTranslation();
  const codes = [...new Set<string>([...CURRENCIES, ...extra.filter((code): code is string => !!code)])];
  return codes.map((code) => ({
    value: code,
    label:
      code === 'BAM'
        ? t('expenseForm.currencyBam')
        : code === 'EUR'
          ? t('expenseForm.currencyEur')
          : code === 'RSD'
            ? t('expenseForm.currencyRsd')
            : code,
  }));
}
