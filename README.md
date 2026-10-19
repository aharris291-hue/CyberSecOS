# CyberSecOS

A personal cybersecurity learning platform that takes a complete beginner to a
job-ready SOC Analyst, with 20% theory and 80% hands-on labs.

- **Backend:** FastAPI, SQLAlchemy, SQLite (`backend/`)
- **Desktop client:** WPF on .NET 9 using MVVM and LiveCharts2 (`frontend/`)

**Status:** Phase 1 (login, dashboard, progress tracking) is complete, with a
course engine of 8 courses and 12 hands-on lessons mapped to Security+, SC-900,
SC-200 and CySA+.

Start with [`docs/01_PROJECT_VISION.md`](docs/01_PROJECT_VISION.md) for the
requirements and [`docs/10_DEVELOPMENT.md`](docs/10_DEVELOPMENT.md) to run it.

## Quick start

```bash
# 1. API
cd backend
python -m pip install -r requirements-dev.txt
python -m uvicorn app.main:create_app --factory --port 8000

# 2. Desktop client (Windows, second terminal)
cd frontend
dotnet run --project src/CyberSecOS.Desktop
```

## Documentation

| Doc | Contents |
|---|---|
| `docs/01`-`09` | Requirements (vision, behaviour, architecture, roadmap, sprint, backlog, done, standards, certifications) |
| `docs/10_DEVELOPMENT.md` | Layout, running, configuration |
| `docs/11_DATA_MODEL.md` | Database tables and the lesson completion rule |
| `docs/12_API.md` | REST API reference |
| `docs/13_CONTENT_GUIDE.md` | Writing lessons and the content schema |
| `docs/14_TESTING.md` | Test suites and coverage |
| `docs/CHANGELOG.md` | What changed, feature by feature |
