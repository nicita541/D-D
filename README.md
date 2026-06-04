# D&D AI Game Master

Русскоязычная RPG-система с ASP.NET Core backend, React frontend, PostgreSQL и локальным Ollama.

## Запуск

Требования: Docker Compose, NVIDIA Container Toolkit и поддерживаемая NVIDIA GPU.

```powershell
Copy-Item .env.example .env
docker compose up -d --build
docker compose ps
```

После запуска:

- frontend: `http://localhost:3000`
- backend API/Swagger: `http://localhost:8080/swagger`
- healthcheck: `http://localhost:8080/health/db`
- Ollama: `http://localhost:11434`

При каждом запуске служба `migrations` идемпотентно применяет недостающие SQL-миграции. Существующий volume `postgres_data` не удаляется и пользовательские тексты не переписываются.

## Локальная разработка

```powershell
docker compose up -d postgres migrations llm
dotnet run --project backend/backend/backend.csproj
Set-Location frontend
npm ci
npm run dev
```

Backend и Vite используют `http://localhost:8080`, Ollama использует модель `qwen2.5:7b`.

## Проверка

```powershell
dotnet build backend/backend.slnx
dotnet test backend/backend/Tests/Tests.csproj
Set-Location frontend
npm run lint
npm run test
npm run build
```

GPU runtime-smoke выполняется локально:

```powershell
docker compose up -d --build
docker compose run --rm migrations
Invoke-RestMethod http://localhost:8080/health/db
powershell -ExecutionPolicy Bypass -File .\scripts\api-critical-smoke.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\closed-loop-game-smoke.ps1
```
