import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';

/**
 * In development the browser talks only to Vite (http://localhost:5173). Every request to
 * /api is forwarded to the backend, so there are no CORS problems and the app never needs to
 * know the backend's address. The address is set in .env.development.local (not in git):
 *
 *   VITE_API_PROXY_TARGET=https://localhost:7123
 */
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, '.', '');
  const target = env.VITE_API_PROXY_TARGET || 'https://localhost:7001';

  const proxy = {
    '/api': {
      target,
      changeOrigin: true,
      // The ASP.NET Core development certificate is self-signed; trust it for local work only.
      secure: false,
    },
  };

  return {
    plugins: [react()],
    server: { port: 5173, proxy },
    preview: { port: 4173, proxy },
  };
});
