import { Navigate, Route, Routes } from 'react-router-dom';
import { LoginPage, RegisterPage } from './auth/AuthPages';
import { ProtectedRoute } from './auth/ProtectedRoute';
import { GamesPage } from './game-states/GamesPage';
import { SetupPage } from './characters/SetupPage';
import { PlayPage } from './play/PlayPage';

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<Navigate to="/games" replace />} />
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />

      <Route element={<ProtectedRoute />}>
        <Route path="/games" element={<GamesPage />} />
        <Route path="/games/:gameStateId/setup" element={<SetupPage />} />
        <Route path="/games/:gameStateId/play" element={<PlayPage />} />
      </Route>

      <Route path="*" element={<Navigate to="/games" replace />} />
    </Routes>
  );
}
