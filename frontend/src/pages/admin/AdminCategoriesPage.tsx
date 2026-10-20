import { createElement, useEffect, useState } from 'react';
import { Check, Pencil, Plus, Trash2, X } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router';
import { refreshPendingCategories } from '../../admin/pendingCategoriesStore';
import { categoriesApi, categoryStatusName } from '../../api/categories';
import type { AdminCategory, CategoryStatus } from '../../api/types';
import { useErrorMessage } from '../../api/useErrorMessage';
import { Alert } from '../../components/ui/Alert';
import { Button } from '../../components/ui/Button';
import { ConfirmDialog } from '../../components/ui/ConfirmDialog';
import { invalidateCategories } from '../../hooks/useCategories';
import { useFormatters } from '../../i18n/format';
import { useLocalizedText } from '../../i18n/useLocalizedText';
import { CategoryFormDialog, RejectCategoryDialog } from '../categories/CategoryFormDialog';
import { iconForKey } from '../expenses/categoryIcons';
import pageStyles from '../Page.module.css';
import styles from './Admin.module.css';
import categoryStyles from '../categories/Categories.module.css';

const TABS: CategoryStatus[] = ['Pending', 'Approved', 'Rejected'];

type DialogState =
  | { kind: 'create' }
  | { kind: 'edit'; category: AdminCategory }
  | { kind: 'approve'; category: AdminCategory }
  | { kind: 'reject'; category: AdminCategory }
  | { kind: 'delete'; category: AdminCategory }
  | null;

/**
 * The administrator keeps the one list of categories everyone uses: reviews users' proposals,
 * adds, renames and deletes categories. A category in use cannot be deleted.
 */
export function AdminCategoriesPage() {
  const { t } = useTranslation();
  const format = useFormatters();
  const localize = useLocalizedText();
  const errorMessage = useErrorMessage();

  // The tab is in the address (?status=Approved); proposals waiting for a decision come first.
  const [params, setParams] = useSearchParams();
  const requested = params.get('status') as CategoryStatus | null;
  const tab: CategoryStatus = requested && TABS.includes(requested) ? requested : 'Pending';
  const selectTab = (next: CategoryStatus) =>
    setParams(next === 'Pending' ? {} : { status: next }, { replace: true, preventScrollReset: true });

  const [version, setVersion] = useState(0);
  const [loaded, setLoaded] = useState<{ version: number; categories: AdminCategory[] | null; error: unknown } | null>(null);
  const [dialog, setDialog] = useState<DialogState>(null);
  const [notice, setNotice] = useState<{ id: number; text: string } | null>(null);

  // One request for every tab: the counts on the tabs come from the same list.
  useEffect(() => {
    const controller = new AbortController();
    categoriesApi.adminList(undefined, controller.signal).then(
      (categories) => setLoaded({ version, categories, error: null }),
      (error: unknown) => {
        if (!controller.signal.aborted) setLoaded((current) => ({ version, categories: current?.categories ?? null, error }));
      },
    );
    return () => controller.abort();
  }, [version]);

  useEffect(() => {
    if (!notice) return;
    const timer = window.setTimeout(() => setNotice(null), 5000);
    return () => window.clearTimeout(timer);
  }, [notice]);

  /** After every change: reload this page, the users' category list and the menu badge. */
  const done = (text: string) => {
    setDialog(null);
    setNotice({ id: Date.now(), text });
    setVersion((value) => value + 1);
    invalidateCategories();
    void refreshPendingCategories();
  };

  const all = loaded?.categories ?? null;
  const byStatus = (status: CategoryStatus) => (all ?? []).filter((category) => categoryStatusName(category.status) === status);
  const shown = byStatus(tab);
  const loading = loaded?.version !== version;
  const name = (category: AdminCategory) => localize(category.name);
  const icon = (category: AdminCategory) =>
    createElement(iconForKey(category.icon, category.name), { size: 18, 'aria-hidden': true });

  return (
    <div className={pageStyles.page}>
      <header className={pageStyles.header}>
        <div className={pageStyles.headerText}>
          <h1 className={pageStyles.title}>{t('adminCategories.title')}</h1>
          <p className={pageStyles.subtitle}>{t('adminCategories.subtitle')}</p>
        </div>
        <Button icon={<Plus size={18} aria-hidden="true" />} onClick={() => setDialog({ kind: 'create' })}>
          {t('adminCategories.add')}
        </Button>
      </header>

      <div className={styles.toolbar}>
        <div className={styles.segmented} role="group" aria-label={t('adminCategories.statusFilter')}>
          {TABS.map((status) => {
            const count = all ? byStatus(status).length : null;
            return (
              <button
                key={status}
                type="button"
                aria-pressed={tab === status}
                className={tab === status ? `${styles.segment} ${styles.segmentActive}` : styles.segment}
                onClick={() => selectTab(status)}
              >
                {t(`adminCategories.tab_${status}`)}
                {count !== null && ` (${count})`}
              </button>
            );
          })}
        </div>
      </div>

      <div aria-live="polite" className={styles.noticeArea}>
        {notice && (
          <Alert key={notice.id} tone="success">
            {notice.text}
          </Alert>
        )}
      </div>

      {loaded?.error != null && (
        <Alert tone="error">
          {t('adminCategories.loadError')} {errorMessage(loaded.error)}
        </Alert>
      )}

      {!all && loaded?.error == null && <p className={styles.muted}>{t('adminCategories.loading')}</p>}

      {all && shown.length === 0 && <p className={styles.emptyBox}>{t(`adminCategories.empty_${tab}`)}</p>}

      {all && shown.length > 0 && (
        <div className={styles.tableCard} aria-busy={loading}>
          <table className={styles.table}>
            <caption className="visually-hidden">{t(`adminCategories.tab_${tab}`)}</caption>
            <thead>
              <tr>
                <th scope="col">{t('adminCategories.colName')}</th>
                {tab === 'Approved' ? (
                  <th scope="col">{t('adminCategories.colUsage')}</th>
                ) : (
                  <th scope="col">{t('adminCategories.colProposedBy')}</th>
                )}
                {tab === 'Rejected' && <th scope="col">{t('adminCategories.colReason')}</th>}
                <th scope="col">
                  <span className="visually-hidden">{t('adminCategories.colActions')}</span>
                </th>
              </tr>
            </thead>
            <tbody>
              {shown.map((category) => (
                <tr key={category.id}>
                  <th scope="row">
                    <span className={styles.userCell}>
                      <span className={categoryStyles.proposalIcon}>{icon(category)}</span>
                      <span className={styles.userText}>
                        <span className={styles.userName}>
                          {name(category)}
                          {category.isSystemDefined && <span className={styles.youBadge}>{t('adminCategories.system')}</span>}
                        </span>
                        {tab === 'Pending' && category.proposedAt && (
                          <span className={styles.userEmail}>
                            {t('adminCategories.proposedOn', { date: format.dateTime(category.proposedAt) })}
                          </span>
                        )}
                      </span>
                    </span>
                  </th>

                  {tab === 'Approved' ? (
                    <td data-label={t('adminCategories.colUsage')}>
                      {category.usageCount > 0 ? t('adminCategories.usage', { count: category.usageCount }) : t('adminCategories.unused')}
                    </td>
                  ) : (
                    <td data-label={t('adminCategories.colProposedBy')}>
                      {category.proposedByName ? (
                        <span className={styles.userText}>
                          <span>{localize(category.proposedByName)}</span>
                          <span className={styles.userEmail}>{category.proposedByEmail}</span>
                        </span>
                      ) : (
                        '–'
                      )}
                    </td>
                  )}

                  {tab === 'Rejected' && (
                    <td data-label={t('adminCategories.colReason')}>
                      {category.rejectionReason}
                      {category.reviewedAt && <span className={styles.subNote}>{format.longDate(category.reviewedAt)}</span>}
                    </td>
                  )}

                  <td className={styles.actionsCell}>
                    {tab === 'Pending' && (
                      <>
                        <Button className={styles.rowButton} onClick={() => setDialog({ kind: 'approve', category })}>
                          <Check size={16} aria-hidden="true" />
                          {t('adminCategories.approve')}
                        </Button>
                        <Button variant="secondary" className={styles.rowButton} onClick={() => setDialog({ kind: 'reject', category })}>
                          <X size={16} aria-hidden="true" />
                          {t('adminCategories.reject')}
                        </Button>
                      </>
                    )}
                    {tab === 'Approved' && (
                      <button
                        type="button"
                        className={styles.iconLink}
                        aria-label={t('adminCategories.edit', { name: name(category) })}
                        title={t('adminCategories.edit', { name: name(category) })}
                        onClick={() => setDialog({ kind: 'edit', category })}
                      >
                        <Pencil size={17} aria-hidden="true" />
                      </button>
                    )}
                    {tab !== 'Pending' && (
                      // Used categories cannot be deleted; the reason is on the wrapper, because a
                      // disabled button shows no tooltip in some browsers.
                      <span
                        title={
                          category.usageCount > 0
                            ? t('adminCategories.inUse', { count: category.usageCount })
                            : t('adminCategories.delete', { name: name(category) })
                        }
                      >
                        <button
                          type="button"
                          className={styles.iconLink}
                          aria-label={
                            category.usageCount > 0
                              ? `${t('adminCategories.delete', { name: name(category) })}. ${t('adminCategories.inUse', { count: category.usageCount })}`
                              : t('adminCategories.delete', { name: name(category) })
                          }
                          onClick={() => setDialog({ kind: 'delete', category })}
                          disabled={category.usageCount > 0}
                        >
                          <Trash2 size={17} aria-hidden="true" />
                        </button>
                      </span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {dialog?.kind === 'create' && (
        <CategoryFormDialog
          title={t('adminCategories.createTitle')}
          submitLabel={t('common.save')}
          busyLabel={t('common.saving')}
          intro={t('adminCategories.createIntro')}
          onClose={() => setDialog(null)}
          onSubmit={async (request) => {
            await categoriesApi.create(request);
            done(t('adminCategories.created', { name: localize(request.name) }));
          }}
        />
      )}

      {dialog?.kind === 'edit' && (
        <CategoryFormDialog
          title={t('adminCategories.editTitle')}
          submitLabel={t('common.save')}
          busyLabel={t('common.saving')}
          initial={{ name: dialog.category.name, icon: dialog.category.icon }}
          intro={dialog.category.usageCount > 0 ? t('adminCategories.editIntro', { count: dialog.category.usageCount }) : undefined}
          onClose={() => setDialog(null)}
          onSubmit={async (request) => {
            await categoriesApi.update(dialog.category.id, request);
            done(t('adminCategories.updated'));
          }}
        />
      )}

      {dialog?.kind === 'approve' && (
        <CategoryFormDialog
          title={t('adminCategories.approveTitle')}
          submitLabel={t('adminCategories.approve')}
          busyLabel={t('adminCategories.approving')}
          initial={{ name: dialog.category.name, icon: dialog.category.icon }}
          intro={t('adminCategories.approveIntro', {
            name: dialog.category.proposedByName ? localize(dialog.category.proposedByName) : '–',
          })}
          onClose={() => setDialog(null)}
          onSubmit={async (request) => {
            await categoriesApi.approve(dialog.category.id, request);
            done(t('adminCategories.approved', { name: localize(request.name) }));
          }}
        />
      )}

      {dialog?.kind === 'reject' && (
        <RejectCategoryDialog
          categoryName={name(dialog.category)}
          onClose={() => setDialog(null)}
          onReject={async (reason) => {
            await categoriesApi.reject(dialog.category.id, reason);
            done(t('adminCategories.rejected', { name: name(dialog.category) }));
          }}
        />
      )}

      {dialog?.kind === 'delete' && (
        <ConfirmDialog
          title={t('adminCategories.deleteTitle')}
          confirmLabel={t('adminCategories.deleteConfirm')}
          busyLabel={t('confirm.deleting')}
          danger
          onClose={() => setDialog(null)}
          onConfirm={async () => {
            await categoriesApi.remove(dialog.category.id);
            done(t('adminCategories.deleted', { name: name(dialog.category) }));
          }}
        >
          <p>{t('adminCategories.deleteText', { name: name(dialog.category) })}</p>
        </ConfirmDialog>
      )}
    </div>
  );
}
