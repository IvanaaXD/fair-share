import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import { THEME_STORAGE_KEY, ThemeContext, type Theme } from './ThemeContext';

/**
 * The user's own choice if they made one on this device, otherwise the operating system's
 * setting. The same rule runs in index.html before React starts, so the page never flashes
 * in the wrong theme while loading.
 */
function getInitialTheme(): Theme {
  try {
    const saved = localStorage.getItem(THEME_STORAGE_KEY);
    if (saved === 'light' || saved === 'dark') {
      return saved;
    }
  } catch {
    // Storage blocked - fall back to the system setting.
  }
  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [theme, setTheme] = useState<Theme>(getInitialTheme);

  // tokens.css switches every color on this one attribute.
  useEffect(() => {
    document.documentElement.dataset.theme = theme;
  }, [theme]);

  const toggleTheme = useCallback(() => {
    setTheme((current) => {
      const next: Theme = current === 'light' ? 'dark' : 'light';
      try {
        // Saved only when the user chooses; until then the system setting is followed.
        localStorage.setItem(THEME_STORAGE_KEY, next);
      } catch {
        // Not saved - the choice still applies until the page is closed.
      }
      return next;
    });
  }, []);

  const value = useMemo(() => ({ theme, toggleTheme }), [theme, toggleTheme]);

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}
