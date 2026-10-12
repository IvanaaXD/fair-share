import { createContext } from 'react';
import type { Profile } from '../api/types';

/**
 * loading       - checking a saved session when the app starts
 * authenticated - signed in, `user` is set
 * anonymous     - not signed in
 */
export type AuthStatus = 'loading' | 'authenticated' | 'anonymous';

export interface AuthContextValue {
  status: AuthStatus;
  user: Profile | null;
  login: (email: string, password: string, remember: boolean) => Promise<void>;
  logout: () => Promise<void>;
  /** Reloads the profile, e.g. after the user changes their name or picture. */
  reloadProfile: () => Promise<void>;
}

export const AuthContext = createContext<AuthContextValue | null>(null);
