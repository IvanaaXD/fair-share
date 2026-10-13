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
  profileImageUrl: string | null;
  role: UserRole;
}

// ---------- errors (ProblemDetails, see ExceptionHandlingMiddleware) ----------

export interface ProblemDetails {
  status?: number;
  title?: string;
  detail?: string;
  /** Validation errors by field name, e.g. { "Email": ["E-mail адреса није исправна."] }. */
  errors?: Record<string, string[]>;
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
}
