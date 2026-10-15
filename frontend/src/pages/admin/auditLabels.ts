/**
 * The audit log stores the controller and action names ("Expense.Create", "AdminUser.Block").
 * These are the ones the app knows how to say in words; anything else is shown as it is.
 */
export const AUDIT_ENTITIES = [
  'Auth',
  'User',
  'Profile',
  'Expense',
  'ExpenseReceipt',
  'Category',
  'Budget',
  'Group',
  'GroupExpense',
  'GroupExpenseReceipt',
  'Comment',
  'Settlement',
  'Payment',
  'PaymentCard',
  'Qr',
  'Notification',
  'AdminUser',
] as const;

export type AuditEntity = (typeof AUDIT_ENTITIES)[number];

export const AUDIT_VERBS = [
  'Login',
  'Logout',
  'Register',
  'ConfirmEmail',
  'ResendConfirmation',
  'ForgotPassword',
  'ResetPassword',
  'ChangePassword',
  'Create',
  'CreateGroup',
  'Add',
  'AddMember',
  'Update',
  'UpdateGroup',
  'Delete',
  'DeleteGroup',
  'Remove',
  'RemoveMember',
  'LeaveGroup',
  'Upload',
  'UploadImage',
  'DeleteImage',
  'Generate',
  'MarkSettled',
  'SendReminder',
  'PayWithCard',
  'Parse',
  'MarkAsRead',
  'MarkAllAsRead',
  'Block',
  'Unblock',
] as const;

export type AuditVerb = (typeof AUDIT_VERBS)[number];

export function isAuditEntity(value: string): value is AuditEntity {
  return (AUDIT_ENTITIES as readonly string[]).includes(value);
}

export function isAuditVerb(value: string): value is AuditVerb {
  return (AUDIT_VERBS as readonly string[]).includes(value);
}

/** "Expense.Create" -> ["Expense", "Create"] */
export function splitAction(action: string): [string, string] {
  const dot = action.indexOf('.');
  return dot < 0 ? [action, ''] : [action.slice(0, dot), action.slice(dot + 1)];
}

/** The backend stores UTC; a value without a zone ("2026-10-14T10:34:00") is read as UTC too. */
export function utcMoment(value: string): string {
  return /[zZ]|[+-]\d\d:?\d\d$/.test(value) ? value : `${value}Z`;
}
