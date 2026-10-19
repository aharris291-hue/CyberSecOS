# Changelog

## Phase 1 - Core Application (in progress)

### Project structure
- Monorepo with `backend/` (FastAPI) and `frontend/` (WPF, MVVM) per the architecture document.
- Health endpoint `GET /api/health`.
- GitHub Actions CI: PyTest on Ubuntu, build and xUnit on Windows.

### Database
- SQLAlchemy models for users, certifications and objectives, courses, lessons, quizzes, progress, quiz attempts and study activity (see `11_DATA_MODEL.md`).
- Tables are created on startup; SQLite foreign keys are enforced.

### Login and user profiles
- First-run account creation and sign-in (`/api/auth/*`) with bcrypt hashing, JWT sessions and lockout after 5 failed attempts.
- Profile read/update and password change (`/api/users/me`).
- Desktop: application shell with view-model-first navigation, login/first-run page, profile page.

### Course engine and quizzes
- 8 courses (one per focus domain) and 12 lessons, each with learning objectives, theory, examples, a quiz, a practical lab and an expected outcome; 19% theory / 81% lab time.
- Certification catalog (Security+, SC-900, SC-200, CySA+) with every lesson mapped to exam objectives.
- Idempotent YAML seeding at startup; content standards enforced by tests (`13_CONTENT_GUIDE.md`).
- Quiz grading with a 70% pass mark, explanations and recorded attempts.
- Desktop: course catalog and lesson pages with an interactive quiz.
