# Changelog

## Phase 1 - Core Application (in progress)

### Project structure
- Monorepo with `backend/` (FastAPI) and `frontend/` (WPF, MVVM) per the architecture document.
- Health endpoint `GET /api/health`.
- GitHub Actions CI: PyTest on Ubuntu, build and xUnit on Windows.
