# Changelog

## Phase 1 - Core Application (complete)

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

### Progress tracking
- Theory read and lab completion with notes and actual minutes; lessons complete when the lab is done and the quiz is passed.
- `/api/progress`: totals, hands-on share, streaks, weekly goal, per-course progress, certification readiness, recent activity, next lesson.
- `/api/progress/activity`: daily study minutes, bucketed in the learner's time zone.
- Desktop: lesson progress checklist, lab notes and completion, and a Progress page.

### Dashboard
- `GET /api/dashboard` returns everything the home screen needs in one call.
- Desktop dashboard (new home page): greeting, up-next lesson, headline stats, weekly goal, and LiveCharts2 charts for daily study time, theory vs hands-on, progress by domain and certification readiness.

### Testing
- End-to-end learner journey test; malformed server responses surface as friendly errors.
- CI runs on pull requests and pushes to `main` only, avoiding duplicate runs.
- Test inventory in `14_TESTING.md`.

### Windows build
- CI publishes a self-contained Windows build (`CyberSecOS-windows` artifact) with the backend and a `Start CyberSecOS.bat` launcher.
- The login screen re-checks the server when you press Sign in, so starting the API late no longer needs an app restart.
- Quick start guide: `15_WINDOWS_QUICKSTART.md`.
