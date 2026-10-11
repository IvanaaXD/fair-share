import { createBrowserRouter, RouterProvider } from 'react-router';
import { AppLayout } from './components/layout/AppLayout';
import { NotFoundPage } from './pages/NotFoundPage';
import { OverviewPage } from './pages/OverviewPage';
import { PlaceholderPage } from './pages/PlaceholderPage';

/**
 * All pages of the app. Pages inside AppLayout get the sidebar; the login and registration
 * pages will be added next to it (outside AppLayout) in the authentication step.
 */
const router = createBrowserRouter([
  {
    path: '/',
    element: <AppLayout />,
    children: [
      { index: true, element: <OverviewPage /> },
      { path: 'expenses', element: <PlaceholderPage titleKey="expenses" /> },
      { path: 'groups', element: <PlaceholderPage titleKey="groups" /> },
      { path: 'groups/:groupId', element: <PlaceholderPage titleKey="groups" /> },
      { path: 'budgets', element: <PlaceholderPage titleKey="budgets" /> },
      { path: 'analytics', element: <PlaceholderPage titleKey="analytics" /> },
      { path: 'notifications', element: <PlaceholderPage titleKey="notifications" /> },
      { path: 'profile', element: <PlaceholderPage titleKey="profile" /> },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
]);

export default function App() {
  return <RouterProvider router={router} />;
}
