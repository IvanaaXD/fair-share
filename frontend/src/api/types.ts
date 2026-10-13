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
