# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Personal website (thomasplant.ca) — a full-stack monorepo with:
- **Frontend**: React 19 + TypeScript + Vite (`main.client/`)
- **Backend**: ASP.NET Core 10 C# (`main.Server/`)
- **Database**: PostgreSQL 18.3 (via Docker)

## Commands

### Frontend (`main.client/`)
```bash
npm run dev       # Vite dev server
npm run build     # tsc -b && vite build
npm run lint      # ESLint
npm run preview   # Preview production build
```

### Backend (`main.Server/`)
```bash
dotnet build      # Build the .NET project
dotnet run        # Run locally (http://localhost:5281)
dotnet publish    # Publish for deployment
```

### Docker (from repo root)
```bash
docker compose up --build   # Build and start all services (backend + PostgreSQL)
docker compose down         # Stop services
```

## Architecture

### Request Flow
In production, the ASP.NET Core backend serves both the API and the compiled frontend static assets. The SPA fallback (`/index.html`) handles all unmatched routes so client-side routing works correctly.

In development, the frontend runs on a separate Vite dev server. The backend's SPA Proxy forwards frontend requests to `https://localhost:63369`.

### Frontend Structure
- `src/main.tsx` — React root with `<BrowserRouter>`
- `src/routes.tsx` — Route definitions (`/` → Home, `/photos` → PhotosDashboard)
- `src/pages/` — Page components
- `src/styles/` — Global SCSS (`index.scss`) and CSS Modules per component

### Backend Structure
- `Program.cs` — App configuration; registers controllers, serves static files, SPA fallback
- `Controllers/` — API controllers
- The backend uses controller-based routing (not minimal APIs)

### Database
PostgreSQL connection string is injected via environment variable `ConnectionStrings__DefaultConnection` in Docker. Credentials come from a `.env` file at the repo root (not committed).

### Docker Build
The `Dockerfile` uses a multi-stage build: Node.js 20 + .NET SDK for the build stage, lean ASP.NET Core runtime for the final image. The frontend is compiled as part of the backend Docker build.
