import { useCallback, useEffect, useId, useState, type FormEvent, type KeyboardEvent } from 'react';
import { MessageSquare, Send, Trash2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { commentsApi } from '../../api/groupExpenses';
import type { ExpenseComment } from '../../api/types';
import { useErrorMessage } from '../../api/useErrorMessage';
import { Alert } from '../../components/ui/Alert';
import { Avatar } from '../../components/ui/Avatar';
import { Button } from '../../components/ui/Button';
import { initials, memberColor } from '../../groups/groupUtils';
import { useFormatters } from '../../i18n/format';
import { useLocalizedText } from '../../i18n/useLocalizedText';
import { notificationTypeName } from '../../notifications/notificationTypes';
import { onNotificationCreated } from '../../notifications/realtime';
import styles from './ExpenseComments.module.css';

/** The same limit as CreateCommentRequestValidator and the database column. */
const MAX_LENGTH = 1000;
/** The counter appears only near the limit, so it does not distract while typing. */
const COUNTER_FROM = 800;

/** The backend stores UTC; a value without a zone is read as UTC too. */
function asUtc(value: string): string {
  return /[zZ]|[+-]\d\d:?\d\d$/.test(value) ? value : `${value}Z`;
}

interface ExpenseCommentsProps {
  groupId: string;
  expenseId: string;
  currentUserId: string;
  /** The group owner may delete any comment (moderation); others only their own. */
  isOwner: boolean;
  colorOf: Map<string, string>;
  /** A comment was added or deleted - the list behind the dialog updates its counter. */
  onChanged: () => void;
}

type Loaded = { comments: ExpenseComment[] | null; error: unknown };

/** The conversation under a group expense: who wrote what and when, and a box for a new comment. */
export function ExpenseComments({ groupId, expenseId, currentUserId, isOwner, colorOf, onChanged }: ExpenseCommentsProps) {
  const { t } = useTranslation();
  const errorMessage = useErrorMessage();
  const localize = useLocalizedText();
  const format = useFormatters();
  const titleId = useId();
  const fieldId = useId();

  const [loaded, setLoaded] = useState<Loaded>({ comments: null, error: null });
  const [version, setVersion] = useState(0);
  const [text, setText] = useState('');
  const [sending, setSending] = useState(false);
  const [sendError, setSendError] = useState<string | null>(null);
  const [confirmingId, setConfirmingId] = useState<string | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    commentsApi.list(groupId, expenseId, controller.signal).then(
      (comments) => setLoaded({ comments, error: null }),
      (error: unknown) => {
        if (!controller.signal.aborted) setLoaded((current) => ({ comments: current.comments, error }));
      },
    );
    return () => controller.abort();
  }, [groupId, expenseId, version]);

  // Someone else commented while the dialog is open: the conversation is loaded again, and so
  // is the list behind the dialog (its comment counter).
  const reload = useCallback(() => setVersion((value) => value + 1), []);
  useEffect(
    () =>
      onNotificationCreated((notification) => {
        if (notificationTypeName(notification.type) === 'GroupExpenseComment') {
          reload();
          onChanged();
        }
      }),
    [reload, onChanged],
  );

  const trimmed = text.trim();
  const tooLong = text.length > MAX_LENGTH;

  async function send(event?: FormEvent) {
    event?.preventDefault();
    if (!trimmed || tooLong || sending) return;
    setSending(true);
    setSendError(null);
    try {
      const comment = await commentsApi.create(groupId, expenseId, trimmed);
      setLoaded((current) => ({ comments: [...(current.comments ?? []), comment], error: null }));
      setText('');
      onChanged();
    } catch (failure) {
      setSendError(errorMessage(failure));
    } finally {
      setSending(false);
    }
  }

  async function remove(comment: ExpenseComment) {
    setDeletingId(comment.id);
    setSendError(null);
    try {
      await commentsApi.remove(groupId, expenseId, comment.id);
      setLoaded((current) => ({ comments: (current.comments ?? []).filter((c) => c.id !== comment.id), error: null }));
      onChanged();
    } catch (failure) {
      setSendError(errorMessage(failure));
    } finally {
      setDeletingId(null);
      setConfirmingId(null);
    }
  }

  // Ctrl+Enter (Cmd+Enter on a Mac) sends; a plain Enter starts a new line.
  function handleKeyDown(event: KeyboardEvent<HTMLTextAreaElement>) {
    if (event.key === 'Enter' && (event.ctrlKey || event.metaKey)) {
      void send();
    }
  }

  const comments = loaded.comments;

  return (
    <section className={styles.section} aria-labelledby={titleId}>
      <h3 id={titleId} className={styles.title}>
        <MessageSquare size={18} aria-hidden="true" />
        {comments ? t('comments.titleCount', { count: comments.length }) : t('comments.title')}
      </h3>

      {loaded.error != null && !comments && (
        <Alert tone="error">
          {t('comments.loadError')} {errorMessage(loaded.error)}
        </Alert>
      )}

      {!comments && loaded.error == null && <p className={styles.muted}>{t('comments.loading')}</p>}

      {comments && comments.length === 0 && <p className={styles.muted}>{t('comments.empty')}</p>}

      {comments && comments.length > 0 && (
        <ol className={styles.list}>
          {comments.map((comment) => {
            const mine = comment.userId === currentUserId;
            const name = mine ? t('comments.you') : localize(`${comment.firstName} ${comment.lastName}`);
            const canDelete = mine || isOwner;
            const when = asUtc(comment.createdAt);
            return (
              <li key={comment.id} className={mine ? `${styles.comment} ${styles.mine}` : styles.comment}>
                <Avatar
                  size="sm"
                  initials={localize(initials(comment.firstName, comment.lastName))}
                  color={colorOf.get(comment.userId) ?? memberColor(5)}
                />
                <div className={styles.bubble}>
                  <div className={styles.meta}>
                    <span className={styles.author}>{name}</span>
                    <time dateTime={when} className={styles.time}>
                      {format.dateTime(when)}
                    </time>
                  </div>
                  {/* Plain text: React escapes it, line breaks are kept by CSS. */}
                  <p className={styles.text}>{comment.text}</p>

                  {canDelete &&
                    (confirmingId === comment.id ? (
                      <div className={styles.confirm} role="group" aria-label={t('comments.deleteQuestion')}>
                        <span>{t('comments.deleteQuestion')}</span>
                        <button
                          type="button"
                          className={styles.linkDanger}
                          onClick={() => void remove(comment)}
                          disabled={deletingId === comment.id}
                        >
                          {deletingId === comment.id ? t('comments.deleting') : t('comments.deleteYes')}
                        </button>
                        <button type="button" className={styles.link} onClick={() => setConfirmingId(null)}>
                          {t('common.cancel')}
                        </button>
                      </div>
                    ) : (
                      <button
                        type="button"
                        className={styles.deleteButton}
                        aria-label={t('comments.delete')}
                        title={t('comments.delete')}
                        onClick={() => setConfirmingId(comment.id)}
                      >
                        <Trash2 size={15} aria-hidden="true" />
                      </button>
                    ))}
                </div>
              </li>
            );
          })}
        </ol>
      )}

      <form className={styles.form} onSubmit={(event) => void send(event)} noValidate>
        <label htmlFor={fieldId} className="visually-hidden">
          {t('comments.newLabel')}
        </label>
        <div className={tooLong ? `${styles.control} ${styles.controlInvalid}` : styles.control}>
          <textarea
            id={fieldId}
            className={styles.textarea}
            rows={2}
            value={text}
            placeholder={t('comments.placeholder')}
            onChange={(event) => setText(event.target.value)}
            onKeyDown={handleKeyDown}
            aria-invalid={tooLong || undefined}
            aria-describedby={`${fieldId}-hint`}
            disabled={sending}
          />
          <Button
            type="submit"
            className={styles.sendButton}
            disabled={!trimmed || tooLong || sending}
            aria-label={t('comments.send')}
            title={t('comments.send')}
          >
            <Send size={17} aria-hidden="true" />
            <span className={styles.sendText}>{sending ? t('comments.sending') : t('comments.send')}</span>
          </Button>
        </div>
        <p id={`${fieldId}-hint`} className={tooLong ? styles.error : styles.hint}>
          {tooLong
            ? t('comments.tooLong', { max: MAX_LENGTH })
            : text.length >= COUNTER_FROM
              ? `${text.length}/${MAX_LENGTH}`
              : t('comments.hint')}
        </p>
        <div aria-live="polite">{sendError && <Alert tone="error">{sendError}</Alert>}</div>
      </form>
    </section>
  );
}
