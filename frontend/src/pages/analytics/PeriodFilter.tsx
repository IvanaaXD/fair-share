import { useState, type FormEvent } from 'react';
import { CalendarRange } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Button } from '../../components/ui/Button';
import { todayDateOnly } from '../../i18n/format';
import { PRESETS, validateCustomRange, type PeriodSelection, type RangeError } from './analyticsPeriod';
import styles from './Analytics.module.css';

const RANGE_ERRORS: Record<RangeError, 'analytics.errorRequired' | 'analytics.errorOrder' | 'analytics.errorFuture' | 'analytics.errorTooLong'> = {
  required: 'analytics.errorRequired',
  order: 'analytics.errorOrder',
  future: 'analytics.errorFuture',
  tooLong: 'analytics.errorTooLong',
};

/** First day of the current month, "YYYY-MM-01" - the default start of a custom period. */
function monthStart(today: string) {
  return `${today.slice(0, 7)}-01`;
}

interface PeriodFilterProps {
  selection: PeriodSelection;
  onSelect: (selection: PeriodSelection) => void;
}

/**
 * The ready-made periods plus "from - to", in one row above everything they scope. Used by the
 * personal analytics and by the system statistics of the administration.
 */
export function PeriodFilter({ selection, onSelect }: PeriodFilterProps) {
  const { t } = useTranslation();

  // The "from - to" form is open while a custom period is shown, or after its button is pressed.
  const [customOpen, setCustomOpen] = useState(selection.period === 'Custom');
  const [today] = useState(() => todayDateOnly());
  const [draft, setDraft] = useState({
    from: selection.period === 'Custom' ? selection.from : monthStart(today),
    to: selection.period === 'Custom' ? selection.to : today,
  });
  const [draftError, setDraftError] = useState<RangeError | null>(null);

  function showCustom(event: FormEvent) {
    event.preventDefault();
    const problem = validateCustomRange(draft.from, draft.to);
    setDraftError(problem);
    if (!problem) onSelect({ period: 'Custom', from: draft.from, to: draft.to });
  }

  return (
    <div className={styles.filters}>
      <div className={styles.segmented} role="group" aria-label={t('analytics.periodLabel')}>
        {PRESETS.map((period) => {
          const pressed = !customOpen && selection.period === period;
          return (
            <button
              key={period}
              type="button"
              aria-pressed={pressed}
              className={pressed ? `${styles.segment} ${styles.segmentActive}` : styles.segment}
              onClick={() => {
                setCustomOpen(false);
                setDraftError(null);
                onSelect({ period, from: '', to: '' });
              }}
            >
              {t(`analytics.period_${period}`)}
            </button>
          );
        })}
        <button
          type="button"
          aria-pressed={customOpen}
          aria-expanded={customOpen}
          aria-controls="analytics-custom"
          className={customOpen ? `${styles.segment} ${styles.segmentActive}` : styles.segment}
          onClick={() => setCustomOpen(true)}
        >
          <CalendarRange size={16} aria-hidden="true" />
          {t('analytics.period_Custom')}
        </button>
      </div>

      {customOpen && (
        <form id="analytics-custom" className={styles.customForm} onSubmit={showCustom} noValidate>
          <label className={styles.dateField}>
            <span>{t('analytics.from')}</span>
            <input
              type="date"
              value={draft.from}
              max={draft.to && draft.to < today ? draft.to : today}
              aria-invalid={draftError !== null}
              aria-describedby={draftError ? 'analytics-range-error' : undefined}
              onChange={(event) => setDraft((current) => ({ ...current, from: event.target.value }))}
            />
          </label>
          <label className={styles.dateField}>
            <span>{t('analytics.to')}</span>
            <input
              type="date"
              value={draft.to}
              min={draft.from || undefined}
              max={today}
              aria-invalid={draftError !== null}
              aria-describedby={draftError ? 'analytics-range-error' : undefined}
              onChange={(event) => setDraft((current) => ({ ...current, to: event.target.value }))}
            />
          </label>
          <Button type="submit" variant="secondary">
            {t('analytics.show')}
          </Button>
          {draftError && (
            <p id="analytics-range-error" className={styles.fieldError} role="alert">
              {t(RANGE_ERRORS[draftError])}
            </p>
          )}
        </form>
      )}
    </div>
  );
}
