import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';

// Fonts are bundled with the app (no request to Google Fonts); both include Cyrillic.
import '@fontsource-variable/onest';
import '@fontsource-variable/unbounded';
import './styles/global.css';

// Must run before the first render, so the first frame is already in the right language.
import './i18n';

import App from './App';
import { ThemeProvider } from './theme/ThemeProvider';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ThemeProvider>
      <App />
    </ThemeProvider>
  </StrictMode>,
);
