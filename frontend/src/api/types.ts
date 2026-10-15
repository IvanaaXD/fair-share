/**
 * Shapes of the API requests and responses used by the web app. They mirror the backend
 * DTOs (FairShare.Application/DTOs); property names are camelCase because ASP.NET Core
 * serializes JSON that way.
 */

// ---------- authentication (AuthController) ----------

export interface LoginRequest {
  /** The user's e-mail address - the backend calls it "username". */
  username: string;
  password: string;
}

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  defaultCurrency: string;
}

export interface AuthResponse {
  id: string;
  username: string;
  accessToken: string;
  refreshToken: string;
}

export interface TokenRequest {
  token: string;
}

export interface ResetPasswordRequest {
  token: string;
  newPassword: string;
}

export interface EmailRequest {
  email: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface MessageResponse {
  message: string;
}

// ---------- the signed-in user (ProfileController) ----------

/** "Admin" or "Customer"; older backends without JsonStringEnumConverter send a number. */
export type UserRole = 'Admin' | 'Customer' | number;

export interface Profile {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  defaultCurrency: string;
  bankAccountNumber: string | null;
  /** "/api/users/{id}/profile-image?v=..." - needs the access token. */
  profileImageUrl: string | null;
  role: UserRole;
  /** true for an account with a predefined password (e.g. the seeded admin); optional on older backends. */
  mustChangePassword?: boolean;
}

export interface UpdateProfileRequest {
  firstName: string;
  lastName: string;
  defaultCurrency: string;
  /** Digits, spaces and dashes; empty removes the account. The backend stores only the digits. */
  bankAccountNumber: string | null;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

// ---------- errors (ProblemDetails, see ExceptionHandlingMiddleware) ----------

export interface ProblemDetails {
  status?: number;
  title?: string;
  detail?: string;
  /** Some filters answer with { message } instead of ProblemDetails (RequirePasswordChangeFilter). */
  message?: string;
  /** Validation errors by field name, e.g. { "Email": ["E-mail адреса није исправна."] }. */
  errors?: Record<string, string[]>;
  /** Stable error code (ErrorCodes on the backend), translated by the app, e.g. "groupHasDebts". */
  code?: string;
}

// ---------- lists with paging (PagedResponse<T>) ----------

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
}

// ---------- categories (CategoryController) ----------

export interface Category {
  id: string;
  name: string;
  isSystemDefined: boolean;
  /** Name of a lucide icon, e.g. "shopping-basket"; optional - otherwise guessed from the name. */
  icon?: string | null;
}

// ---------- personal expenses (ExpenseController, ExpenseReceiptController) ----------

export type RecurrenceInterval = 'None' | 'Daily' | 'Weekly' | 'Monthly' | 'Yearly';

export const RECURRENCE_INTERVALS: Exclude<RecurrenceInterval, 'None'>[] = ['Daily', 'Weekly', 'Monthly', 'Yearly'];

export type ExpenseSortField = 'Date' | 'Amount';

export interface Expense {
  id: string;
  categoryId: string;
  categoryName: string;
  amount: number;
  currency: string;
  /** ISO date-time in UTC; only the date part matters ("2026-10-12T00:00:00Z"). */
  date: string;
  nextOccurrenceDate: string | null;
  description: string | null;
  latitude: number | null;
  longitude: number | null;
  /** "/api/expenses/{id}/receipt?v=..." - needs the access token, see useAuthorizedImage. */
  receiptImageUrl: string | null;
  isRecurring: boolean;
  recurrenceInterval: RecurrenceInterval;
}

/** Body of POST /api/expenses and PUT /api/expenses/{id}. The receipt is uploaded separately. */
export interface SaveExpenseRequest {
  amount: number;
  currency: string;
  /** "YYYY-MM-DD" */
  date: string;
  categoryId: string;
  description: string | null;
  latitude: number | null;
  longitude: number | null;
  isRecurring: boolean;
  recurrenceInterval: RecurrenceInterval;
}

/** Query string of GET /api/expenses; every field is optional. */
export interface ExpenseQuery {
  page?: number;
  pageSize?: number;
  /** "YYYY-MM-DD", both days included */
  from?: string;
  to?: string;
  categoryId?: string;
  search?: string;
  minAmount?: number;
  maxAmount?: number;
  isRecurring?: boolean;
  sortBy?: ExpenseSortField;
  sortDescending?: boolean;
}

export interface ImageUrlResponse {
  url: string;
}

// ---------- groups (GroupController, SettlementController) ----------

/** "Owner" or "Member"; older backends without JsonStringEnumConverter send 0 / 1. */
export type GroupRole = 'Owner' | 'Member' | number;

export interface GroupMember {
  userId: string;
  firstName: string;
  lastName: string;
  email: string;
  role: GroupRole;
  joinedAt: string;
}

export interface Group {
  id: string;
  name: string;
  description: string | null;
  /** Fixed when the group is created - all expenses and settlements are in it. */
  currency: string;
  createdAt: string;
  members: GroupMember[];
}

export interface CreateGroupRequest {
  name: string;
  description: string | null;
  currency: string;
}

export interface UpdateGroupRequest {
  name: string;
  description: string | null;
}

export interface AddGroupMemberRequest {
  email: string;
}

/** GET /api/groups/{id}/settlements/balances - positive: the group owes the member. */
export interface MemberBalance {
  userId: string;
  firstName: string;
  lastName: string;
  netBalance: number;
}

// ---------- group expenses (GroupExpenseController, GroupExpenseReceiptController) ----------

/** Older backends without JsonStringEnumConverter send 0 / 1 / 2. */
export type SplitType = 'Equal' | 'Percentage' | 'ExactAmount';

export interface ExpenseSplit {
  userId: string;
  firstName: string;
  lastName: string;
  amount: number;
  percentage: number | null;
}

export interface GroupExpense {
  id: string;
  groupId: string;
  categoryId: string;
  categoryName: string;
  paidByUserId: string;
  paidByName: string;
  amount: number;
  description: string | null;
  /** "/api/groups/{g}/expenses/{e}/receipt?v=..." - needs the access token. */
  receiptImageUrl: string | null;
  date: string;
  splitType: SplitType | number;
  splits: ExpenseSplit[];
}

export interface SplitParticipant {
  userId: string;
  /** Only for ExactAmount. */
  amount: number | null;
  /** Only for Percentage. */
  percentage: number | null;
}

/** Body of POST and PUT - an edit sends the whole expense again and the splits are recalculated. */
export interface SaveGroupExpenseRequest {
  categoryId: string;
  paidByUserId: string;
  amount: number;
  description: string | null;
  /** "YYYY-MM-DD" */
  date: string;
  splitType: SplitType;
  participants: SplitParticipant[];
}

// ---------- settlements (SettlementController) ----------

export type SettlementStatus = 'Proposed' | 'Settled';

export interface Settlement {
  id: string;
  groupId: string;
  debtorUserId: string;
  debtorName: string;
  creditorUserId: string;
  creditorName: string;
  amount: number;
  status: SettlementStatus | number;
  createdAt: string;
  /** When the creditor last sent a reminder (UTC); null if never. One per 24 hours. */
  lastReminderSentAt?: string | null;
}

// ---------- payments (QrController, PaymentCardController, PaymentController) ----------

/** GET .../settlements/{id}/qr/payload - the IPS QR text and the fields it is made of. */
export interface QrPayload {
  settlementTransactionId: string;
  payload: string;
  recipientAccount: string;
  recipientName: string;
  amount: number;
  currency: string;
  purpose: string | null;
  referenceCode: string;
}

/** POST /api/qr/parse - what was read from a QR code, and the settlement it belongs to. */
export interface ParsedQr {
  isValid: boolean;
  errors: string[];
  recipientAccount: string | null;
  recipientName: string | null;
  amount: number | null;
  currency: string | null;
  paymentCode: string | null;
  purpose: string | null;
  reference: string | null;
  matchedSettlementId: string | null;
  matchedGroupId: string | null;
  matchedSettlementStatus: SettlementStatus | number | null;
  canMarkAsSettled: boolean;
}

export interface PaymentCard {
  id: string;
  brand: string;
  last4Digits: string;
  expiryMonth: number;
  expiryYear: number;
  isExpired: boolean;
}

/** The number and CVV go to the payment simulator through the backend and are never stored. */
export interface AddPaymentCardRequest {
  cardNumber: string;
  expiryMonth: number;
  expiryYear: number;
  cvv: string;
  holderName: string | null;
}

export type PaymentStatus = 'Pending' | 'Succeeded' | 'Failed';

export interface PaymentResult {
  id: string;
  settlementTransactionId: string;
  status: PaymentStatus | number;
  /** card_declined, insufficient_funds, expired_card, invalid_card, processing_error */
  failureReason: string | null;
  amount: number;
  currency: string;
  cardBrand: string;
  cardLast4Digits: string;
  createdAt: string;
}

// ---------- budgets (BudgetController) ----------

export interface Budget {
  id: string;
  categoryId: string;
  categoryName: string;
  monthlyLimit: number;
  /** "YYYY-MM" */
  month: string;
  /** Sum of the user's expenses in the category in that month (calculated by the backend). */
  currentSpending: number;
  percentageUsed: number;
}

export interface CreateBudgetRequest {
  categoryId: string;
  monthlyLimit: number;
  month: string;
}

export interface UpdateBudgetRequest {
  monthlyLimit: number;
}

// ---------- notifications (NotificationController) ----------

export type NotificationType =
  | 'AccountActivation'
  | 'GroupInvite'
  | 'PasswordReset'
  | 'PaymentConfirmation'
  | 'BudgetExceeded'
  | 'SettlementSuggested'
  | 'SettlementCompleted'
  | 'GroupMembershipChanged'
  | 'RecurringExpenseCreated'
  | 'GroupExpenseAdded'
  | 'DebtReminder';

export interface AppNotification {
  id: string;
  /** A name, or a number on backends without JsonStringEnumConverter. */
  type: NotificationType | number;
  message: string;
  isRead: boolean;
  createdAt: string;
}

// ---------- analytics ----------

export type AnalyticsPeriod = 'LastWeek' | 'LastMonth' | 'Last3Months' | 'Last6Months' | 'LastYear' | 'Custom';

/** Chosen by the backend from the length of the period: up to 31 days by day, up to 92 by week. */
export type TimeGranularity = 'Day' | 'Week' | 'Month';

export interface AnalyticsQuery {
  period: AnalyticsPeriod;
  /** "YYYY-MM-DD", only with period=Custom (both days included, at most one year apart) */
  from?: string;
  to?: string;
  /** Only expenses in this currency are counted, so KM and EUR are never added together. */
  currency?: string;
}

export interface SpendingTimePoint {
  periodStart: string;
  periodEnd: string;
  total: number;
}

export interface CategorySpending {
  categoryId: string;
  categoryName: string;
  total: number;
  count: number;
  /** Share of the period's total, 0-100. */
  percentage: number;
  previousPeriodTotal: number;
}

export interface SpendingAnalytics {
  from: string;
  to: string;
  granularity: TimeGranularity | number;
  totalSpent: number;
  expenseCount: number;
  /** The period of the same length right before the chosen one. */
  previousFrom: string;
  previousTo: string;
  previousPeriodTotal: number;
  /** All amounts are in this currency (older backends do not send it). */
  currency?: string;
  /** Expenses in other currencies, converted with the rates of `ratesUpdatedAt`. */
  convertedExpenseCount?: number;
  /** Expenses left out because no rate was known for their currency. */
  unconvertedExpenseCount?: number;
  ratesUpdatedAt?: string | null;
  /** null when nothing was spent in the previous period. */
  changePercentage: number | null;
  timeline: SpendingTimePoint[];
  byCategory: CategorySpending[];
}

export interface ExpenseLocation {
  expenseId: string;
  amount: number;
  currency: string;
  date: string;
  description: string | null;
  categoryName: string;
  latitude: number;
  longitude: number;
}

// ---------- administration (AdminController, AdminUserController) ----------

export interface AdminTotals {
  users: number;
  blockedUsers: number;
  admins: number;
  groups: number;
  personalExpenses: number;
  groupExpenses: number;
  proposedSettlements: number;
  settledSettlements: number;
  /** Status name ("Pending", "Succeeded", "Failed"...) -> number of payments. */
  paymentsByStatus: Record<string, number>;
}

export interface AdminTimePoint {
  periodStart: string;
  periodEnd: string;
  newUsers: number;
  newGroups: number;
  personalExpenses: number;
  groupExpenses: number;
  settlements: number;
  payments: number;
}

export type AdminSeries = Exclude<keyof AdminTimePoint, 'periodStart' | 'periodEnd'>;

export interface AdminStatistics {
  from: string;
  to: string;
  granularity: TimeGranularity | number;
  /** All-time state of the system, not only the chosen period. */
  totals: AdminTotals;
  /** New records per day/week/month in the chosen period. */
  timeline: AdminTimePoint[];
}

export interface AdminUser {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  role: UserRole;
  isBlocked: boolean;
  mustChangePassword: boolean;
  createdAt: string;
}

export interface AdminUserQuery {
  search?: string;
  isBlocked?: boolean;
  page?: number;
  pageSize?: number;
}

export interface AuditLogEntry {
  id: string;
  userId: string;
  userFullName: string;
  userEmail: string;
  /** "Controller.Action", e.g. "AdminUser.Block" or "Auth.Login". */
  action: string;
  /** The controller name, e.g. "Expense", "Group", "AdminUser". */
  entityType: string;
  entityId: string | null;
  timestamp: string;
}

export interface AuditLogQuery {
  userId?: string;
  entityType?: string;
  /** "YYYY-MM-DD" */
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}
