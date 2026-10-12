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
