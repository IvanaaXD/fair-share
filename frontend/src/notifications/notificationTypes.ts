import {
  ArrowLeftRight,
  CircleCheck,
  CreditCard,
  KeyRound,
  ReceiptText,
  type LucideIcon,
  PiggyBank,
  Repeat,
  UserCog,
  UserPlus,
  UserRoundCheck,
} from 'lucide-react';
import type { AppNotification, NotificationType } from '../api/types';

/** The order of NotificationType on the backend, for backends that send numbers. */
const TYPES: NotificationType[] = [
  'AccountActivation',
  'GroupInvite',
  'PasswordReset',
  'PaymentConfirmation',
  'BudgetExceeded',
  'SettlementSuggested',
  'SettlementCompleted',
  'GroupMembershipChanged',
  'RecurringExpenseCreated',
  'GroupExpenseAdded',
];

export const NOTIFICATION_ICONS: Record<NotificationType, LucideIcon> = {
  AccountActivation: UserRoundCheck,
  GroupInvite: UserPlus,
  PasswordReset: KeyRound,
  PaymentConfirmation: CreditCard,
  BudgetExceeded: PiggyBank,
  SettlementSuggested: ArrowLeftRight,
  SettlementCompleted: CircleCheck,
  GroupMembershipChanged: UserCog,
  RecurringExpenseCreated: Repeat,
  GroupExpenseAdded: ReceiptText,
};

/** Notifications that need attention are marked in amber (budget over limit, someone owes). */
export const WARNING_TYPES = new Set<NotificationType>(['BudgetExceeded', 'SettlementSuggested']);

export function notificationTypeName(type: AppNotification['type']): NotificationType {
  return typeof type === 'number' ? (TYPES[type] ?? 'GroupInvite') : type;
}
