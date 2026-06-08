import { Navigate, Route, Routes } from 'react-router-dom';
import { LoginPage, RegisterPage } from './auth/AuthPages';
import { ProtectedRoute } from './auth/ProtectedRoute';
import { AdminRoute } from './auth/AdminRoute';
import { HomePage } from './player/HomePage';
import { CharactersPage } from './player/CharactersPage';
import { CharacterEditorPage } from './player/CharacterEditorPage';
import { StoriesPage } from './player/StoriesPage';
import { LaunchPage } from './player/LaunchPage';
import { GamesPage } from './game-states/GamesPage';
import { SetupPage } from './characters/SetupPage';
import { SetupRedirect } from './characters/SetupRedirect';
import { PlayPage } from './play/PlayPage';
import { GameInvitePage, InviteAcceptPage } from './invites/InvitePages';
import { CharactersManagePage } from './manage/CharactersManagePage';
import { WorldManagePage } from './manage/WorldManagePage';
import { StoryManagePage } from './manage/StoryManagePage';
import { EncountersManagePage } from './manage/EncountersManagePage';
import { HistoryManagePage } from './manage/HistoryManagePage';
import { CampaignsAdminPage } from './admin/CampaignsAdminPage';

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<Navigate to="/home" replace />} />
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route path="/invites/:token" element={<InviteAcceptPage />} />

      <Route element={<ProtectedRoute />}>
        <Route path="/home" element={<HomePage />} />
        <Route path="/characters" element={<CharactersPage />} />
        <Route path="/characters/new" element={<CharacterEditorPage />} />
        <Route path="/characters/:characterId/edit" element={<CharacterEditorPage />} />
        <Route path="/stories" element={<StoriesPage />} />
        <Route path="/launch" element={<LaunchPage />} />
        <Route path="/games" element={<GamesPage />} />
        <Route path="/setup" element={<SetupRedirect />} />
        <Route path="/games/:gameStateId/setup" element={<SetupPage />} />
        <Route path="/games/:gameStateId/invite" element={<GameInvitePage />} />
        <Route path="/games/:gameStateId/play" element={<PlayPage />} />
        <Route path="/games/:gameStateId/manage/characters" element={<CharactersManagePage />} />
        <Route path="/games/:gameStateId/manage/world" element={<WorldManagePage />} />
        <Route path="/games/:gameStateId/manage/story" element={<StoryManagePage />} />
        <Route path="/games/:gameStateId/manage/encounters" element={<EncountersManagePage />} />
        <Route path="/games/:gameStateId/manage/history" element={<HistoryManagePage />} />
        <Route element={<AdminRoute />}>
          <Route path="/admin/campaigns" element={<CampaignsAdminPage />} />
        </Route>
      </Route>

      <Route path="*" element={<Navigate to="/home" replace />} />
    </Routes>
  );
}
