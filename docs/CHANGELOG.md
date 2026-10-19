# Changelog

## Phase 1 - Core Application (in progress)

### Project structure
- Monorepo with `backend/` (FastAPI) and `frontend/` (WPF, MVVM) per the architecture document.
- Health endpoint `GET /api/health`.
- GitHub Actions CI: PyTest on Ubuntu, build and xUnit on Windows.

### Database
- SQLAlchemy models for users, certifications and objectives, courses, lessons, quizzes, progress, quiz attempts and study activity (see `11_DATA_MODEL.md`).
- Tables are created on startup; SQLite foreign keys are enforced.
