import type { ProblemDetails } from './types';

/**
 * Every failed API call ends as an ApiError, so pages handle errors in one way:
 * status code, the server's message, and validation errors per field.
 */
export class ApiError extends Error {
  readonly status: number;
  readonly title?: string;
  readonly detail?: string;
  /** Stable code from the backend (ErrorCodes), translated by useErrorMessage. */
  readonly code?: string;
  /** Field errors keyed by camelCase field name, e.g. { email: ['...'] }. */
  readonly fieldErrors: Record<string, string[]>;

  constructor(status: number, problem: ProblemDetails = {}) {
    super(problem.detail ?? problem.message ?? problem.title ?? `HTTP ${status}`);
    this.name = 'ApiError';
    this.status = status;
    this.title = problem.title;
    this.detail = problem.detail ?? problem.message;
    this.code = problem.code;
    this.fieldErrors = normalizeFieldErrors(problem.errors);
  }

  /** The request never reached the server (no network, server not running). */
  static network(): ApiError {
    return new ApiError(0);
  }

  get isNetworkError() {
    return this.status === 0;
  }

  /** The account was blocked by an administrator (403 from the account filter or the login). */
  get isAccountBlocked() {
    return this.status === 403 && (this.code === 'accountBlocked' || /blocked|блокиран/i.test(this.detail ?? ''));
  }
}

/** The backend sends C# property names ("Email", "NewPassword"); the forms use camelCase. */
function normalizeFieldErrors(errors: Record<string, string[]> | undefined): Record<string, string[]> {
  const result: Record<string, string[]> = {};
  if (!errors) {
    return result;
  }
  for (const [key, messages] of Object.entries(errors)) {
    const field = key.length > 0 ? key[0].toLowerCase() + key.slice(1) : key;
    result[field] = messages;
  }
  return result;
}
