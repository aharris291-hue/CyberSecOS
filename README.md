# CyberSecOS

A personal cybersecurity learning platform that takes a complete beginner to a
job-ready SOC Analyst, with 20% theory and 80% hands-on labs.

- **Backend:** FastAPI, SQLAlchemy, SQLite (`backend/`)
- **Desktop client:** WPF on .NET 9 using MVVM and LiveCharts2 (`frontend/`)

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
