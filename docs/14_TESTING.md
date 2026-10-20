# Testing

Definition of Done (`07_DEFINITION_OF_DONE.md`) requires tests for every
feature and a passing build. CI (`.github/workflows/ci.yml`) runs both suites on
every pull request and on pushes to `main`.

## Backend (PyTest, `backend/tests`)

| File | Covers |
|---|---|
| `test_health.py` | App starts, health endpoint |
| `test_database.py` | Tables, JSON columns, unique constraints, cascades |
| `test_auth.py` | First-run registration, single-account rule, password policy, hashing, login, lockout, token tampering and expiry |
| `test_users.py` | Profile read/update and password change, including that a change invalidates older tokens and returns a working fresh one |
| `test_content_standards.py` | Every lesson has the six required sections, valid quizzes, certification mapping, 20/80 split, every focus domain and certification covered |
| `test_seed.py` | Seeding and idempotent re-seeding that keeps progress |
| `test_quiz_grading.py` | Scoring, rounding, pass mark, unanswered questions |
| `test_courses_api.py` | Catalog, lesson detail without answers, quiz submission |
| `test_progress.py` | Theory/lab/quiz state machine, summary, streaks, week, time zones, readiness |
| `test_dashboard.py` | Dashboard payload for new and active learners |
| `test_exams.py` | Exam sampling across objectives, 80% grading, per-objective breakdown, submission validation and attempt history |
| `test_exam_targets.py` | Set/get/clear a per-certification target date, countdown maths, and its surfacing in dashboard readiness |
| `test_planner.py` | Study plan: remaining-work schedule packing, weekly-target limits, pace verdicts (on track / behind / overdue / no date), soonest-exam ordering, and the low-goal bound |
| `test_learner_journey.py` | End-to-end: sign up, study a lesson, see it on the dashboard |

Each test gets a fresh SQLite database in a temp directory. bcrypt uses a low
cost factor in tests only (`CYBERSECOS_BCRYPT_ROUNDS=4`).

## Desktop (xUnit, `frontend/tests/CyberSecOS.Core.Tests`)

View models and the API client are tested without WPF:

- `ApiClientTests`: snake_case JSON, bearer header, error mapping, offline server, malformed responses.
- `LoginViewModelTests`, `ShellTests`, `ProfileViewModelTests`: first-run vs sign-in, validation, navigation, sign-out, expired sessions.
- `CoursesViewModelTests`, `LessonViewModelTests`: catalog, lesson sections, quiz selection, grading and retry.
- `ProgressTrackingTests`: theory/lab commands, checklist, Progress page.
- `DashboardViewModelTests`: tiles, chart data, next lesson, empty state.
- `ExamsViewModelTests`: certification selection, length capping, timer, answering, grading, focus areas, the setup/taking/results flow, and target-date set/clear with countdown.
- `PlannerViewModelTests`: plan load, on-track / behind / complete headlines, per-certification pace phrasing, week summaries, and navigation to a lesson or the exam dates.
- `MarkdownTextTests`: lesson-theory parsing into paragraphs with bold and inline-code runs, including unmatched markers and wrapped lines.

`Fakes/FakeApi` is an in-memory `ICyberSecApi`; `Fakes/StubHttpHandler` fakes HTTP.

## Not yet automated

- XAML views and LiveCharts2 converters (need a Windows UI test runner; see backlog).
