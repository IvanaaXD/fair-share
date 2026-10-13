import { createBrowserRouter, RouterProvider } from 'react-router';
import { PublicOnly, RequireAuth } from './auth/RouteGuards';
import { AppLayout } from './components/layout/AppLayout';
import { AuthLayout } from './components/layout/AuthLayout';
import { ConfirmEmailPage } from './pages/auth/ConfirmEmailPage';
import { EmailRequestPage } from './pages/auth/EmailRequestPage';
import { LoginPage } from './pages/auth/LoginPage';
import { RegisterPage } from './pages/auth/RegisterPage';
import { ResetPasswordPage } from './pages/auth/ResetPasswordPage';
import { ExpensesPage } from './pages/expenses/ExpensesPage';
import { NotFoundPage } from './pages/NotFoundPage';
import { OverviewPage } from './pages/OverviewPage';
import { PlaceholderPage } from './pages/PlaceholderPage';

/**
 * All pages of the app.
 *
 * - Before sign-in (AuthLayout): login, registration and "forgot password" are only for
 *   signed-out users; the pages opened from e-mail links work in both states.
 * - The app itself (AppLayout) requires a signed-in user; anyone else is sent to /login
 *   and comes back to the same page after signing in.
 */
const router = createBrowserRouter([
  {
    element: <AuthLayout />,
    children: [
      {
        element: <PublicOnly />,
        children: [
          { path: 'login', element: <LoginPage /> },
          { path: 'register', element: <RegisterPage /> },
          { path: 'forgot-password', element: <EmailRequestPage mode="forgot" /> },
        ],
      },
      // Paths are fixed by the backend: AuthSettings.FrontendBaseUrl + /confirm-email?token=...
      { path: 'confirm-email', element: <ConfirmEmailPage /> },
      { path: 'reset-password', element: <ResetPasswordPage /> },
      { path: 'resend-confirmation', element: <EmailRequestPage mode="resend" /> },
    ],
  },
  {
    element: <RequireAuth />,
    children: [
      {
        path: '/',
        element: <AppLayout />,
        children: [
          { index: true, element: <OverviewPage /> },
          { path: 'expenses', element: <ExpensesPage /> },
          { path: 'groups', element: <PlaceholderPage titleKey="groups" /> },
          { path: 'groups/:groupId', element: <PlaceholderPage titleKey="groups" /> },
          { path: 'budgets', element: <PlaceholderPage titleKey="budgets" /> },
          { path: 'analytics', element: <PlaceholderPage titleKey="analytics" /> },
          { path: 'notifications', element: <PlaceholderPage titleKey="notifications" /> },
          { path: 'profile', element: <PlaceholderPage titleKey="profile" /> },
          { path: '*', element: <NotFoundPage /> },
        ],
      },
    ],
  },
]);

export default function App() {
  return <RouterProvider router={router} />;
}
