# D&D Frontend

React + Vite SPA для текущего backend playable-flow.

## Локальная разработка

```powershell
cd E:\D&D\frontend
npm install
npm run dev
```

Vite проксирует `/api` и `/health` на `http://localhost:8080`.

## Docker

```powershell
cd E:\D&D
docker compose up -d --build postgres backend llm frontend
```

Frontend будет доступен на `http://localhost:3000`.
Nginx раздаёт SPA и проксирует `/api` на backend service внутри compose network.

## Проверки

```powershell
cd E:\D&D\frontend
npm run lint
npm run build
```

В этом проекте npm scripts запускают локальные CLI через `node node_modules/...`, чтобы путь `E:\D&D` не ломал Windows `.cmd` wrappers на символе `&`.
