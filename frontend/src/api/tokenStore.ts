/**
 * Where the tokens live.
 *
 * - The ACCESS token (valid for hours, sent with every request) is kept only in memory: it is
 *   lost on reload and recreated from the refresh token, so it is never written to disk.
 * - The REFRESH token is what keeps the user signed in. With "Remember me" it goes to
 *   localStorage (survives closing the browser), otherwise to sessionStorage (gone when the
 *   tab is closed). The backend rotates it on every use and closes all sessions if an old one
 *   is reused, which limits the damage if it is ever stolen.
 */
const REFRESH_KEY = 'fairshare.refreshToken';

let accessToken: string | null = null;

function safeGet(storage: Storage): string | null {
  try {
    return storage.getItem(REFRESH_KEY);
  } catch {
    return null;
  }
}

function safeRemove(storage: Storage) {
  try {
    storage.removeItem(REFRESH_KEY);
  } catch {
    // Storage blocked - nothing to remove.
  }
}

export const tokenStore = {
  getAccessToken: () => accessToken,

  getRefreshToken: () => safeGet(localStorage) ?? safeGet(sessionStorage),

  /** Remembered if the current refresh token is in localStorage (used when it is rotated). */
  isRemembered: () => safeGet(localStorage) !== null,

  save(tokens: { accessToken: string; refreshToken: string }, remember: boolean) {
    accessToken = tokens.accessToken;
    safeRemove(localStorage);
    safeRemove(sessionStorage);
    try {
      (remember ? localStorage : sessionStorage).setItem(REFRESH_KEY, tokens.refreshToken);
    } catch {
      // Storage blocked: the session lasts until the page is reloaded.
    }
  },

  clear() {
    accessToken = null;
    safeRemove(localStorage);
    safeRemove(sessionStorage);
  },
};
