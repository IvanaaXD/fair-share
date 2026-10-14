import { useState } from 'react';
import { LocateFixed, MapPin, X } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useFormatters } from '../../i18n/format';
import styles from './ExpenseDialog.module.css';

export interface Coordinates {
  latitude: number;
  longitude: number;
}

interface LocationFieldProps {
  value: Coordinates | null;
  onChange: (value: Coordinates | null) => void;
  disabled?: boolean;
}

type Problem = 'denied' | 'unavailable' | 'unsupported';

/**
 * "Add my location": where the expense was made, from the browser's location (the user is asked
 * for permission the first time). The analytics map shows expenses with a location.
 * Works on https and on localhost only - browsers do not give the location to plain http pages.
 */
export function LocationField({ value, onChange, disabled }: LocationFieldProps) {
  const { t } = useTranslation();
  const format = useFormatters();
  const [locating, setLocating] = useState(false);
  const [problem, setProblem] = useState<Problem | null>(null);

  function locate() {
    if (!('geolocation' in navigator)) {
      setProblem('unsupported');
      return;
    }
    setLocating(true);
    setProblem(null);
    navigator.geolocation.getCurrentPosition(
      (position) => {
        setLocating(false);
        onChange({
          latitude: Math.round(position.coords.latitude * 1e6) / 1e6,
          longitude: Math.round(position.coords.longitude * 1e6) / 1e6,
        });
      },
      (error) => {
        setLocating(false);
        setProblem(error.code === error.PERMISSION_DENIED ? 'denied' : 'unavailable');
      },
      { enableHighAccuracy: false, timeout: 10_000, maximumAge: 60_000 },
    );
  }

  const coordinates = value
    ? `${format.number(value.latitude, 5)}, ${format.number(value.longitude, 5)}`
    : '';
  const mapUrl = value
    ? `https://www.openstreetmap.org/?mlat=${value.latitude}&mlon=${value.longitude}#map=17/${value.latitude}/${value.longitude}`
    : '';

  return (
    <div className={styles.location}>
      <span className={styles.locationLabel}>{t('expenseForm.location')}</span>
      {value ? (
        <div className={styles.locationValue}>
          <MapPin size={18} aria-hidden="true" />
          <span className={styles.locationText}>
            {t('expenseForm.locationSaved')}{' '}
            <a href={mapUrl} target="_blank" rel="noreferrer" className={styles.locationLink}>
              {coordinates}
            </a>
          </span>
          <button
            type="button"
            className={styles.locationRemove}
            onClick={() => onChange(null)}
            disabled={disabled}
            aria-label={t('expenseForm.locationRemove')}
            title={t('expenseForm.locationRemove')}
          >
            <X size={16} aria-hidden="true" />
          </button>
        </div>
      ) : (
        <button type="button" className={styles.locationButton} onClick={locate} disabled={disabled || locating}>
          <LocateFixed size={17} aria-hidden="true" />
          {locating ? t('expenseForm.locating') : t('expenseForm.locationAdd')}
        </button>
      )}
      {problem && (
        <p className={styles.locationProblem} role="alert">
          {t(`expenseForm.location_${problem}`)}
        </p>
      )}
    </div>
  );
}
