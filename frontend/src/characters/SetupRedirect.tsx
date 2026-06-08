import { Navigate, useParams } from 'react-router-dom';

export function SetupRedirect() {
  const { gameStateId = '' } = useParams();
  if (!gameStateId) {
    return <Navigate to="/games" replace />;
  }

  return <Navigate to={`/games/${gameStateId}/manage/characters`} replace />;
}
