import { Navigate, Outlet, useLocation, type Location } from 'react-router';
import { FullPageLoader } from '../components/ui/FullPageLoader';
import { useAuth } from './useAuth';

/** Pages of the signed-in app. Anyone else is sent to the login page, which returns them here. */
export function RequireAuth() {
  const { status } = useAuth();
  const location = useLocation();

  if (status === 'loading') {
    return <FullPageLoader />;
  }
  if (status === 'anonymous') {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }
  return <Outlet />;
}

/** Login, registration and "forgot password" make no sense when already signed in. */
export function PublicOnly() {
  const { status } = useAuth();
  const location = useLocation();

  if (status === 'loading') {
    return <FullPageLoader />;
  }
  if (status === 'authenticated') {
    const from = (location.state as { from?: Location } | null)?.from;
    return <Navigate to={from ? `${from.pathname}${from.search}` : '/'} replace />;
  }
  return <Outlet />;
}
