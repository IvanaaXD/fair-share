import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import { authApi } from '../api/auth';
import { onSessionExpired, refreshSession } from '../api/client';
import { tokenStore } from '../api/tokenStore';
import type { Profile } from '../api/types';
import { AuthContext, type AuthStatus } from './AuthContext';

/**
 * Restores a saved session once per page load. Kept outside the component, so React's
 * StrictMode (which runs effects twice in development) cannot start a second refresh with the
 * same refresh token - the backend would see that as a stolen token and close every session.
 */
let restorePromise: Promise<Profile | null> | null = null;

function restoreSession(): Promise<Profile | null> {
  if (!restorePromise) {
    restorePromise = (async () => {
      if (!(await refreshSession())) {
        return null;
      }
      try {
        return await authApi.getProfile();
      } catch {
        tokenStore.clear();
        return null;
      }
    })();
  }
  return restorePromise;
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<Profile | null>(null);
  // With a saved refresh token there is a session to restore; without it the user is
  // simply not signed in and the login page can show immediately.
  const [status, setStatus] = useState<AuthStatus>(() => (tokenStore.getRefreshToken() ? 'loading' : 'anonymous'));

  const signOutLocally = useCallback(() => {
    tokenStore.clear();
    setUser(null);
    setStatus('anonymous');
  }, []);

  useEffect(() => {
    onSessionExpired(signOutLocally);
  }, [signOutLocally]);

  useEffect(() => {
    if (status !== 'loading') {
      return;
    }
    let cancelled = false;
    void restoreSession().then((profile) => {
      if (cancelled) {
        return;
      }
      setUser(profile);
      setStatus(profile ? 'authenticated' : 'anonymous');
    });
    return () => {
      cancelled = true;
    };
  }, [status]);

  const login = useCallback(async (email: string, password: string, remember: boolean) => {
    const tokens = await authApi.login({ username: email.trim(), password });
    tokenStore.save(tokens, remember);
    try {
      const profile = await authApi.getProfile();
      setUser(profile);
      setStatus('authenticated');
    } catch (error) {
      tokenStore.clear();
      throw error;
    }
  }, []);

  const logout = useCallback(async () => {
    const refreshToken = tokenStore.getRefreshToken();
    signOutLocally();
    // Revokes the refresh token on the server; the user is signed out locally either way.
    if (refreshToken) {
      try {
        await authApi.logout(refreshToken);
      } catch {
        // Offline or already expired - nothing else to do.
      }
    }
  }, [signOutLocally]);

  const reloadProfile = useCallback(async () => {
    setUser(await authApi.getProfile());
  }, []);

  const value = useMemo(
    () => ({ status, user, login, logout, reloadProfile, setProfile: setUser }),
    [status, user, login, logout, reloadProfile],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
