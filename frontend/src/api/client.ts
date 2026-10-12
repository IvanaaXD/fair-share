import { ApiError } from './ApiError';
import { tokenStore } from './tokenStore';
import type { AuthResponse, ProblemDetails } from './types';

/**
 * All requests go to /api on the same address as the web app. In development Vite forwards
 * them to the backend (vite.config.ts), in Docker Nginx does - so the app never needs to know
 * the backend's address and the browser never makes a cross-origin request.
 */
const API_BASE = '/api';

interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE';
  body?: unknown;
  /** false for sign-in, registration and the other public endpoints. */
  auth?: boolean;
  signal?: AbortSignal;
}

let sessionExpiredHandler: (() => void) | null = null;

/** AuthProvider registers what happens when the session can no longer be renewed. */
export function onSessionExpired(handler: () => void) {
  sessionExpiredHandler = handler;
}

async function send(path: string, options: RequestOptions, accessToken: string | null): Promise<Response> {
  const headers: Record<string, string> = { Accept: 'application/json' };
  if (options.body !== undefined) {
    headers['Content-Type'] = 'application/json';
  }
  if (accessToken) {
    headers.Authorization = `Bearer ${accessToken}`;
  }

  try {
    return await fetch(`${API_BASE}${path}`, {
      method: options.method ?? 'GET',
      headers,
      body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
      signal: options.signal,
    });
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') {
      throw error;
    }
    throw ApiError.network();
  }
}

async function toApiError(response: Response): Promise<ApiError> {
  let problem: ProblemDetails = {};
  try {
    problem = (await response.json()) as ProblemDetails;
  } catch {
    // Not JSON (e.g. a proxy error page) - the status code is enough.
  }
  return new ApiError(response.status, problem);
}

async function parse<T>(response: Response): Promise<T> {
  if (response.status === 204) {
    return undefined as T;
  }
  const text = await response.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

// ---------- refreshing the access token ----------

let refreshInFlight: Promise<boolean> | null = null;

/**
 * Exchanges the refresh token for a new pair. Several requests can fail with 401 at the same
 * moment; they all wait for ONE refresh, because a refresh token can be used only once and a
 * second parallel use would look like theft to the backend and close every session.
 */
export function refreshSession(): Promise<boolean> {
  if (!refreshInFlight) {
    refreshInFlight = (async () => {
      const refreshToken = tokenStore.getRefreshToken();
      if (!refreshToken) {
        return false;
      }
      const response = await send('/auth/refresh', { method: 'POST', body: { refreshToken } }, null);
      if (!response.ok) {
        tokenStore.clear();
        return false;
      }
      const tokens = await parse<AuthResponse>(response);
      tokenStore.save(tokens, tokenStore.isRemembered());
      return true;
    })()
      .catch(() => false)
      .finally(() => {
        refreshInFlight = null;
      });
  }
  return refreshInFlight;
}

/**
 * Sends a request and returns the parsed JSON. A 401 on a protected endpoint triggers one
 * token refresh and one retry; if that fails too, the session has ended.
 */
export async function apiRequest<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const useAuth = options.auth !== false;

  let response = await send(path, options, useAuth ? tokenStore.getAccessToken() : null);

  if (response.status === 401 && useAuth) {
    const refreshed = await refreshSession();
    if (!refreshed) {
      sessionExpiredHandler?.();
      throw await toApiError(response);
    }
    response = await send(path, options, tokenStore.getAccessToken());
  }

  if (!response.ok) {
    throw await toApiError(response);
  }
  return parse<T>(response);
}
