import { useState, type ReactNode } from 'react';
import { FileSpreadsheet, FileText } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import type { DownloadedFile } from '../../api/client';
import { browserTimeZone, saveFile, type ReportFormat, type ReportOptions } from '../../api/reports';
import { useErrorMessage } from '../../api/useErrorMessage';
import { Alert } from '../../components/ui/Alert';
import { Button } from '../../components/ui/Button';
import { Dialog } from '../../components/ui/Dialog';
import styles from './ExportReportDialog.module.css';

const FORMATS: { value: ReportFormat; icon: typeof FileText }[] = [
  { value: 'Pdf', icon: FileText },
  { value: 'Csv', icon: FileSpreadsheet },
];

const STORAGE_KEY = 'fairshare.reportFormat';

/** The format chosen last time, so a user who always wants CSV does not pick it every time. */
function rememberedFormat(): ReportFormat {
  try {
    return localStorage.getItem(STORAGE_KEY) === 'Csv' ? 'Csv' : 'Pdf';
  } catch {
    return 'Pdf';
  }
}

function rememberFormat(format: ReportFormat) {
  try {
    localStorage.setItem(STORAGE_KEY, format);
  } catch {
    // Storage blocked (private window) - the choice is simply not remembered.
  }
}

interface ExportReportDialogProps {
  title: string;
  /** What the report covers (period, group...). */
  children: ReactNode;
  /** Downloads the report; the dialog adds the language and time zone. */
  download: (options: ReportOptions) => Promise<DownloadedFile>;
  /** Used only if the server does not send a file name. */
  fileName: (format: ReportFormat) => string;
  onClose: () => void;
}

/** Lets the user choose PDF or CSV and downloads the report in the app's current language. */
export function ExportReportDialog({ title, children, download, fileName, onClose }: ExportReportDialogProps) {
  const { t, i18n } = useTranslation();
  const errorMessage = useErrorMessage();
  const [format, setFormat] = useState<ReportFormat>(rememberedFormat);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function start() {
    setBusy(true);
    setError(null);
    rememberFormat(format);
    try {
      const file = await download({ format, lang: i18n.language, timeZone: browserTimeZone() });
      saveFile(file, fileName(format));
      onClose();
    } catch (failure) {
      setError(errorMessage(failure));
      setBusy(false);
    }
  }

  return (
    <Dialog
      title={title}
      onClose={onClose}
      dismissible={!busy}
      footer={
        <div className={styles.buttons}>
          <Button variant="secondary" onClick={onClose} disabled={busy}>
            {t('common.cancel')}
          </Button>
          <Button onClick={() => void start()} disabled={busy} data-autofocus>
            {busy ? t('reports.preparing') : t('reports.download')}
          </Button>
        </div>
      }
    >
      <div className={styles.body}>
        <div className={styles.intro}>{children}</div>

        <fieldset className={styles.formats} disabled={busy}>
          <legend className={styles.legend}>{t('reports.format')}</legend>
          {FORMATS.map(({ value, icon: Icon }) => (
            <label key={value} className={styles.option}>
              <input
                type="radio"
                name="reportFormat"
                value={value}
                checked={format === value}
                onChange={() => setFormat(value)}
              />
              <span className={styles.icon} aria-hidden="true">
                <Icon size={22} strokeWidth={1.75} />
              </span>
              <span className={styles.text}>
                <span className={styles.name}>{t(`reports.format${value}`)}</span>
                <span className={styles.hint}>{t(`reports.format${value}Hint`)}</span>
              </span>
            </label>
          ))}
        </fieldset>

        <p className={styles.note}>{t('reports.languageNote')}</p>

        <div aria-live="polite">{error && <Alert tone="error">{error}</Alert>}</div>
      </div>
    </Dialog>
  );
}
