import { createBrowserRouter, RouterProvider } from 'react-router';
import { PublicOnly, RequireAuth } from './auth/RouteGuards';
import { AppLayout } from './components/layout/AppLayout';
import { AuthLayout } from './components/layout/AuthLayout';
import { FullPageLoader } from './components/ui/FullPageLoader';
import { ChangePasswordPage } from './pages/auth/ChangePasswordPage';
import { ConfirmEmailPage } from './pages/auth/ConfirmEmailPage';
import { EmailRequestPage } from './pages/auth/EmailRequestPage';
import { LoginPage } from './pages/auth/LoginPage';
import { RegisterPage } from './pages/auth/RegisterPage';
import { ResetPasswordPage } from './pages/auth/ResetPasswordPage';
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
    // Shown while a page that is loaded on demand (lazy) is downloaded on the first visit.
    HydrateFallback: FullPageLoader,
    children: [
      {
        // Before anything else, an account with a predefined password must choose its own.
        element: <AuthLayout />,
        children: [{ path: 'change-password', element: <ChangePasswordPage /> }],
      },
      {
        path: '/',
        element: <AppLayout />,
        children: [
          { index: true, element: <OverviewPage /> },
          // The bigger sections are separate files that the browser downloads only when the
          // section is opened for the first time, so the first visit loads less code.
          {
            path: 'expenses',
            lazy: () => import('./pages/expenses/ExpensesPage').then((module) => ({ Component: module.ExpensesPage })),
          },
          {
            path: 'groups',
            lazy: () => import('./pages/groups/GroupsPage').then((module) => ({ Component: module.GroupsPage })),
          },
          {
            path: 'groups/:groupId',
            lazy: () => import('./pages/groups/GroupPage').then((module) => ({ Component: module.GroupPage })),
          },
          {
            path: 'budgets',
            lazy: () => import('./pages/budgets/BudgetsPage').then((module) => ({ Component: module.BudgetsPage })),
          },
          { path: 'analytics', element: <PlaceholderPage titleKey="analytics" /> },
          {
            path: 'notifications',
            lazy: () =>
              import('./pages/notifications/NotificationsPage').then((module) => ({ Component: module.NotificationsPage })),
          },
          {
            path: 'profile',
            lazy: () => import('./pages/profile/ProfilePage').then((module) => ({ Component: module.ProfilePage })),
          },
          { path: '*', element: <NotFoundPage /> },
        ],
      },
    ],
  },
]);

export default function App() {
  return <RouterProvider router={router} />;
}
