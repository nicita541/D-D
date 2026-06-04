import { Navigate, useParams } from 'react-router-dom';

export function SetupRedirect() {
  const { gameStateId = '' } = useParams();
  return <Navigate to={`/games/${gameStateId}/manage/characters`} replace />;
}
