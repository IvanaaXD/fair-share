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
  /** Sent as JSON, except FormData (file uploads), which the browser encodes itself. */
  body?: unknown;
  /** 'blob' for images; 'file' for downloads (the blob plus the server's file name); JSON otherwise. */
  responseType?: 'json' | 'blob' | 'file';
  /** false for sign-in, registration and the other public endpoints. */
  auth?: boolean;
  signal?: AbortSignal;
}

/** A downloaded file and the name the server gave it, if any. */
export interface DownloadedFile {
  blob: Blob;
  fileName: string | null;
}

/**
 * The file name from a Content-Disposition header. ASP.NET Core sends both forms:
 * filename="a.pdf" and filename*=UTF-8''a.pdf - the second one can hold any characters.
 */
function fileNameFrom(header: string | null): string | null {
  if (!header) {
    return null;
  }
  const encoded = /filename\*\s*=\s*UTF-8''([^;]+)/i.exec(header);
  if (encoded) {
    try {
      return decodeURIComponent(encoded[1].trim());
    } catch {
      // Not valid percent-encoding - fall back to the plain form below.
    }
  }
  const plain = /filename\s*=\s*"?([^";]+)"?/i.exec(header);
  return plain ? plain[1].trim() : null;
}

let sessionExpiredHandler: (() => void) | null = null;
let accountBlockedHandler: (() => void) | null = null;

/** AuthProvider registers what happens when the session can no longer be renewed. */
export function onSessionExpired(handler: () => void) {
  sessionExpiredHandler = handler;
}

/** AuthProvider registers what happens when an administrator blocks the signed-in account. */
export function onAccountBlocked(handler: () => void) {
  accountBlockedHandler = handler;
}

async function send(path: string, options: RequestOptions, accessToken: string | null): Promise<Response> {
  const isForm = options.body instanceof FormData;
  const headers: Record<string, string> = {};
  if (!options.responseType || options.responseType === 'json') {
    headers.Accept = 'application/json';
  }
  // For FormData the browser sets multipart/form-data together with the boundary.
  if (options.body !== undefined && !isForm) {
    headers['Content-Type'] = 'application/json';
  }
  if (accessToken) {
    headers.Authorization = `Bearer ${accessToken}`;
  }

  try {
    return await fetch(`${API_BASE}${path}`, {
      method: options.method ?? 'GET',
      headers,
      body: isForm ? (options.body as FormData) : options.body !== undefined ? JSON.stringify(options.body) : undefined,
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

async function parse<T>(response: Response, responseType: RequestOptions['responseType'] = 'json'): Promise<T> {
  if (response.status === 204) {
    return undefined as T;
  }
  if (responseType === 'blob') {
    return (await response.blob()) as T;
  }
  if (responseType === 'file') {
    const file: DownloadedFile = {
      blob: await response.blob(),
      fileName: fileNameFrom(response.headers.get('Content-Disposition')),
    };
    return file as T;
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
    const error = await toApiError(response);
    // Blocked while signed in: every further request would fail, so the session ends here.
    if (useAuth && error.isAccountBlocked) {
      accountBlockedHandler?.();
    }
    throw error;
  }
  return parse<T>(response, options.responseType);
}

/**
 * "?search=stan&page=2" from an object. Empty values are left out, so the backend uses its
 * defaults for them.
 */
export function toQueryString(params: Record<string, string | number | boolean | null | undefined>): string {
  const search = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== '') {
      search.set(key, String(value));
    }
  }
  const text = search.toString();
  return text ? `?${text}` : '';
}

/**
 * The backend returns file addresses that already start with /api (e.g.
 * "/api/expenses/{id}/receipt?v=..."), while apiRequest adds /api itself.
 */
export function apiPath(url: string): string {
  return url.startsWith(API_BASE + '/') ? url.slice(API_BASE.length) : url;
}
