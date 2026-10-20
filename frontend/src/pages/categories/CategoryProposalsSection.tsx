import { createElement, useEffect, useState } from 'react';
import { Plus } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { categoriesApi, categoryStatusName } from '../../api/categories';
import type { CategoryProposal } from '../../api/types';
import { useErrorMessage } from '../../api/useErrorMessage';
import { Alert } from '../../components/ui/Alert';
import { Button } from '../../components/ui/Button';
import { useFormatters } from '../../i18n/format';
import { useLocalizedText } from '../../i18n/useLocalizedText';
import { onNotificationCreated } from '../../notifications/realtime';
import { notificationTypeName } from '../../notifications/notificationTypes';
import { iconForKey } from '../expenses/categoryIcons';
import { CategoryFormDialog } from './CategoryFormDialog';
import styles from './Categories.module.css';

interface CategoryProposalsSectionProps {
  cardClassName: string;
  titleClassName: string;
  onNotice: (text: string) => void;
}

/**
 * Profile: a user proposes a new category and follows the administrator's decisions. A
 * proposal can be chosen for expenses only after it is approved.
 */
export function CategoryProposalsSection({ cardClassName, titleClassName, onNotice }: CategoryProposalsSectionProps) {
  const { t } = useTranslation();
  const errorMessage = useErrorMessage();
  const localize = useLocalizedText();
  const format = useFormatters();

  const [version, setVersion] = useState(0);
  const [loaded, setLoaded] = useState<{ proposals: CategoryProposal[] | null; error: unknown }>({ proposals: null, error: null });
  const [proposing, setProposing] = useState(false);

  useEffect(() => {
    const controller = new AbortController();
    categoriesApi.myProposals(controller.signal).then(
      (proposals) => setLoaded({ proposals, error: null }),
      (error: unknown) => {
        if (!controller.signal.aborted) setLoaded((current) => ({ proposals: current.proposals, error }));
      },
    );
    return () => controller.abort();
  }, [version]);

  // The administrator decided while the page is open: the status changes without a refresh.
  useEffect(
    () =>
      onNotificationCreated((notification) => {
        if (notificationTypeName(notification.type) === 'CategoryReviewed') setVersion((value) => value + 1);
      }),
    [],
  );

  const proposals = loaded.proposals;

  return (
    <section className={cardClassName} aria-labelledby="category-proposals-title">
      <div className={styles.sectionTop}>
        <div>
          <h2 id="category-proposals-title" className={titleClassName}>
            {t('categoryProposals.title')}
          </h2>
          <p className={styles.muted}>{t('categoryProposals.hint')}</p>
        </div>
        <Button variant="secondary" icon={<Plus size={17} aria-hidden="true" />} onClick={() => setProposing(true)}>
          {t('categoryProposals.propose')}
        </Button>
      </div>

      {loaded.error != null && !proposals && (
        <Alert tone="error">
          {t('categoryProposals.loadError')} {errorMessage(loaded.error)}
        </Alert>
      )}

      {proposals && proposals.length === 0 && <p className={styles.muted}>{t('categoryProposals.empty')}</p>}

      {proposals && proposals.length > 0 && (
        <ul className={styles.proposals}>
          {proposals.map((proposal) => {
            const status = categoryStatusName(proposal.status);
            return (
              <li key={proposal.id} className={styles.proposal}>
                <span className={styles.proposalIcon} aria-hidden="true">
                  {createElement(iconForKey(proposal.icon, proposal.name), { size: 18 })}
                </span>
                <span className={styles.proposalText}>
                  <span className={styles.proposalName}>{localize(proposal.name)}</span>
                  <span className={styles.proposalMeta}>
                    {proposal.proposedAt && t('categoryProposals.proposedOn', { date: format.longDate(proposal.proposedAt) })}
                    {proposal.reviewedAt && ` · ${t('categoryProposals.reviewedOn', { date: format.longDate(proposal.reviewedAt) })}`}
                  </span>
                  {status === 'Rejected' && proposal.rejectionReason && (
                    <span className={styles.reason}>{t('categoryProposals.reason', { reason: proposal.rejectionReason })}</span>
                  )}
                </span>
                <span className={`${styles.status} ${styles[`status_${status}`]}`}>{t(`categoryStatus.${status}`)}</span>
              </li>
            );
          })}
        </ul>
      )}

      {proposing && (
        <CategoryFormDialog
          title={t('categoryProposals.dialogTitle')}
          submitLabel={t('categoryProposals.send')}
          busyLabel={t('categoryProposals.sending')}
          intro={t('categoryProposals.dialogIntro')}
          onClose={() => setProposing(false)}
          onSubmit={async (request) => {
            await categoriesApi.create(request);
            setProposing(false);
            setVersion((value) => value + 1);
            onNotice(t('categoryProposals.sent', { name: localize(request.name) }));
          }}
        />
      )}
    </section>
  );
}
