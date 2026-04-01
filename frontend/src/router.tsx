import { createBrowserRouter, Navigate } from 'react-router-dom';
import { AppLayout } from './components/AppLayout/AppLayout';
import { LoginPage } from './auth/LoginPage';
import { UploadPage } from './pages/UploadPage/UploadPage';
import { ListPage } from './pages/ListPage/ListPage';
import { DetailPage } from './pages/DetailPage/DetailPage';
import { useAuth } from './auth/AuthContext';

// eslint-disable-next-line react-refresh/only-export-components
function RequireAuth({ children }: { children: React.ReactNode }) {
  const { token } = useAuth();
  return token ? <>{children}</> : <Navigate to="/login" replace />;
}

export const router = createBrowserRouter([
  {
    path: '/login',
    element: <LoginPage />,
  },
  {
    path: '/',
    element: (
      <RequireAuth>
        <AppLayout />
      </RequireAuth>
    ),
    children: [
      { index: true, element: <ListPage /> },
      { path: 'upload', element: <UploadPage /> },
      { path: 'reconciliations/:jobId', element: <DetailPage /> },
    ],
  },
]);
