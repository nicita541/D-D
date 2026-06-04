import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from './useAuth';

export function AdminRoute() {
  const auth = useAuth();
  return auth.session?.account.role === 'admin' ? <Outlet /> : <Navigate to="/games" replace />;
}
