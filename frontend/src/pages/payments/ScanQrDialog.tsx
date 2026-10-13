import { useCallback, useEffect, useRef, useState, type ReactNode } from 'react';
import { Camera, CameraOff, ImageUp, RotateCcw } from 'lucide-react';
import jsQR from 'jsqr';
import { useTranslation } from 'react-i18next';
import { settlementsApi } from '../../api/groupExpenses';
import { qrApi } from '../../api/payments';
import type { ParsedQr } from '../../api/types';
import { useErrorMessage } from '../../api/useErrorMessage';
import { Alert } from '../../components/ui/Alert';
import { Button } from '../../components/ui/Button';
import { Dialog } from '../../components/ui/Dialog';
import { isSettledStatus } from '../../groups/groupUtils';
import { useFormatters } from '../../i18n/format';
import { useLocalizedText } from '../../i18n/useLocalizedText';
import { formatBankAccount } from '../profile/bankAccount';
import styles from './Payments.module.css';

type Mode = 'camera' | 'upload';

/** Images are scaled down to this width before decoding - plenty for a QR code, and fast. */
const MAX_DECODE_WIDTH = 800;

/** Reads a QR code from pixels; null when there is none. */
function decode(source: CanvasImageSource, width: number, height: number, canvas: HTMLCanvasElement): string | null {
  const scale = Math.min(1, MAX_DECODE_WIDTH / width);
  canvas.width = Math.round(width * scale);
  canvas.height = Math.round(height * scale);
  const context = canvas.getContext('2d', { willReadFrequently: true });
  if (!context) return null;
  context.drawImage(source, 0, 0, canvas.width, canvas.height);
  const pixels = context.getImageData(0, 0, canvas.width, canvas.height);
  return jsQR(pixels.data, pixels.width, pixels.height, { inversionAttempts: 'attemptBoth' })?.data ?? null;
}

interface ScanQrDialogProps {
  onClose: () => void;
  /** A scanned settlement was marked as paid - the page should refresh. */
  onSettled: (message: string) => void;
}

/**
 * Reads an IPS QR code with the camera or from an image, lets the backend parse it
 * (POST /api/qr/parse) and, if it belongs to an open settlement of the user, offers to mark
 * that settlement as paid. Decoding happens in the browser (jsQR), so no image is uploaded.
 */
export function ScanQrDialog({ onClose, onSettled }: ScanQrDialogProps) {
  const { t } = useTranslation();
  const errorMessage = useErrorMessage();
  const localize = useLocalizedText();
  const format = useFormatters();

  const [mode, setMode] = useState<Mode>('camera');
  const [cameraOn, setCameraOn] = useState(false);
  const [problem, setProblem] = useState<string | null>(null);
  const [reading, setReading] = useState(false);
  const [result, setResult] = useState<ParsedQr | null>(null);
  const [marking, setMarking] = useState(false);

  const videoRef = useRef<HTMLVideoElement>(null);
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const streamRef = useRef<MediaStream | null>(null);
  const frameRef = useRef(0);
  const fileRef = useRef<HTMLInputElement>(null);

  const canvas = () => (canvasRef.current ??= document.createElement('canvas'));

  const stopCamera = useCallback(() => {
    cancelAnimationFrame(frameRef.current);
    streamRef.current?.getTracks().forEach((track) => track.stop());
    streamRef.current = null;
    setCameraOn(false);
  }, []);

  // The camera is always released when the dialog closes.
  useEffect(() => stopCamera, [stopCamera]);

  async function handleText(text: string) {
    setReading(true);
    setProblem(null);
    try {
      setResult(await qrApi.parse(text));
    } catch (error) {
      setProblem(errorMessage(error));
    } finally {
      setReading(false);
    }
  }

  async function startCamera() {
    setProblem(null);
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: 'environment' }, audio: false });
      streamRef.current = stream;
      const video = videoRef.current;
      if (!video) return;
      video.srcObject = stream;
      await video.play();
      setCameraOn(true);

      const scan = () => {
        if (!streamRef.current) return;
        if (video.readyState >= video.HAVE_ENOUGH_DATA) {
          const text = decode(video, video.videoWidth, video.videoHeight, canvas());
          if (text) {
            stopCamera();
            void handleText(text);
            return;
          }
        }
        frameRef.current = requestAnimationFrame(scan);
      };
      frameRef.current = requestAnimationFrame(scan);
    } catch {
      stopCamera();
      setProblem(t('scan.cameraError'));
    }
  }

  async function readImage(file: File) {
    setProblem(null);
    setReading(true);
    try {
      const bitmap = await createImageBitmap(file);
      const text = decode(bitmap, bitmap.width, bitmap.height, canvas());
      bitmap.close();
      if (text) {
        await handleText(text);
      } else {
        setProblem(t('scan.notFound'));
      }
    } catch {
      setProblem(t('scan.notFound'));
    } finally {
      setReading(false);
    }
  }

  async function markPaid(parsed: ParsedQr) {
    if (!parsed.matchedGroupId || !parsed.matchedSettlementId) return;
    setMarking(true);
    try {
      await settlementsApi.settle(parsed.matchedGroupId, parsed.matchedSettlementId);
      onSettled(t('scan.marked'));
    } catch (error) {
      setProblem(errorMessage(error));
      setMarking(false);
    }
  }

  const reset = () => {
    setResult(null);
    setProblem(null);
  };

  return (
    <Dialog title={t('scan.title')} onClose={onClose} dismissible={!marking}>
      <div className={styles.dialogBody}>
        {result ? (
          <ScanResult result={result} format={format} localize={localize}>
            {problem && <Alert tone="error">{problem}</Alert>}
            <div className={styles.formButtons}>
              <Button variant="secondary" icon={<RotateCcw size={16} aria-hidden="true" />} onClick={reset} disabled={marking}>
                {t('scan.scanAgain')}
              </Button>
              {result.canMarkAsSettled && (
                <Button onClick={() => void markPaid(result)} disabled={marking}>
                  {marking ? t('settlements.confirming') : t('payment.markPaid')}
                </Button>
              )}
            </div>
          </ScanResult>
        ) : (
          <>
            <div className={styles.tabs} role="group" aria-label={t('scan.modesLabel')}>
              {(['camera', 'upload'] as Mode[]).map((value) => (
                <button
                  key={value}
                  type="button"
                  aria-pressed={mode === value}
                  className={mode === value ? `${styles.tab} ${styles.tabActive}` : styles.tab}
                  onClick={() => {
                    stopCamera();
                    setProblem(null);
                    setMode(value);
                  }}
                >
                  {value === 'camera' ? <Camera size={17} aria-hidden="true" /> : <ImageUp size={17} aria-hidden="true" />}
                  {value === 'camera' ? t('scan.camera') : t('scan.upload')}
                </button>
              ))}
            </div>

            {problem && <Alert tone="error">{problem}</Alert>}
            {reading && <p className={styles.muted} role="status">{t('scan.reading')}</p>}

            {mode === 'camera' ? (
              <div className={styles.camera}>
                <div className={styles.viewfinder}>
                  <video ref={videoRef} muted playsInline className={cameraOn ? styles.video : styles.videoHidden} />
                  {!cameraOn && <Camera size={40} strokeWidth={1.5} aria-hidden="true" className={styles.cameraIcon} />}
                </div>
                <p className={styles.help}>{t('scan.cameraHint')}</p>
                <Button
                  variant={cameraOn ? 'secondary' : 'primary'}
                  icon={cameraOn ? <CameraOff size={17} aria-hidden="true" /> : <Camera size={17} aria-hidden="true" />}
                  onClick={() => (cameraOn ? stopCamera() : void startCamera())}
                >
                  {cameraOn ? t('scan.stop') : t('scan.start')}
                </Button>
              </div>
            ) : (
              <button type="button" className={styles.dropzone} onClick={() => fileRef.current?.click()} disabled={reading}>
                <ImageUp size={28} strokeWidth={1.5} aria-hidden="true" />
                {t('scan.chooseImage')}
              </button>
            )}
            <input
              ref={fileRef}
              type="file"
              accept="image/*"
              className="visually-hidden"
              tabIndex={-1}
              aria-hidden="true"
              onChange={(event) => {
                const file = event.target.files?.[0];
                event.target.value = '';
                if (file) void readImage(file);
              }}
            />
          </>
        )}
      </div>
    </Dialog>
  );
}

interface ScanResultProps {
  result: ParsedQr;
  format: ReturnType<typeof useFormatters>;
  localize: (text: string) => string;
  children: ReactNode;
}

function ScanResult({ result, format, localize, children }: ScanResultProps) {
  const { t } = useTranslation();

  if (!result.isValid) {
    return (
      <>
        <Alert tone="error">
          {t('scan.invalid')}
          <ul className={styles.errorList}>
            {result.errors.map((error) => (
              <li key={error}>{localize(error)}</li>
            ))}
          </ul>
        </Alert>
        {children}
      </>
    );
  }

  const matched = result.matchedSettlementId !== null;
  const settled = result.matchedSettlementStatus !== null && isSettledStatus(result.matchedSettlementStatus);

  return (
    <>
      <h3 className={styles.subTitle}>{t('scan.result')}</h3>
      <dl className={styles.details}>
        {result.recipientName && (
          <>
            <dt>{t('payment.recipient')}</dt>
            <dd>{localize(result.recipientName)}</dd>
          </>
        )}
        {result.recipientAccount && (
          <>
            <dt>{t('payment.account')}</dt>
            <dd className={styles.mono}>{formatBankAccount(result.recipientAccount)}</dd>
          </>
        )}
        {result.amount !== null && (
          <>
            <dt>{t('payment.amount')}</dt>
            <dd className="money">{format.money(result.amount, result.currency ?? 'BAM')}</dd>
          </>
        )}
        {result.purpose && (
          <>
            <dt>{t('payment.purpose')}</dt>
            <dd>{localize(result.purpose)}</dd>
          </>
        )}
        {result.reference && (
          <>
            <dt>{t('payment.reference')}</dt>
            <dd className={styles.mono}>{result.reference}</dd>
          </>
        )}
      </dl>
      <Alert tone={result.canMarkAsSettled ? 'success' : 'info'}>
        {!matched ? t('scan.notMatched') : settled ? t('scan.alreadySettled') : t('scan.matched')}
      </Alert>
      {children}
    </>
  );
}
