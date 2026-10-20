# Changelog

## Phase 2 - More SOC-core depth

### A third lesson for the core environment and tooling courses
- Added one lesson each to Windows, Linux and SIEM (12 courses, 29 lessons), the environments and tool a SOC analyst works in daily.
- New lessons: Windows Active Directory and authentication monitoring (logon types, Kerberos, spraying and lateral movement); Linux auditing and persistence hunting (auditd/journald, cron/systemd/startup files, tracing a process via /proc); and investigating a SIEM alert end-to-end (triage, pivoting, timeline, verdict).
- All keep the six education-standard sections and the ~18/82 theory/lab split; content-standard and coverage tests still pass (217 backend tests).

## Phase 2 - Exam-date tracking

### Target exam dates with a countdown
- A learner can set a target sitting date for each certification and see a live countdown, both on the Practice Exams page and next to certification readiness on the Progress page.
- New `exam_targets` table and endpoints (`GET`/`PUT`/`DELETE` `/api/certifications/{cert_id}/exam/target`); certification readiness in `/api/progress` and `/api/dashboard` now carries `target_date` and `days_until_exam`.
- 8 new backend tests and 6 new desktop view-model tests (205 backend, 100 desktop tests total).

## Phase 2 - Readable lessons

### Rich Markdown rendering of lesson theory
- Lesson theory now renders real formatting in the desktop app: bold terms, inline `code`, and spaced paragraphs, instead of showing the raw `**` and backtick markers.
- A small, dependency-free Markdown reader in `CyberSecOS.Core` (paragraphs, bold, inline code) parses the theory; an attached property fills the view's text inlines. 11 new unit tests cover it (94 desktop tests total).

## Phase 2 - Deeper SOC content

### A second lesson for every single-lesson course
- Added one lesson each to SIEM, Threat Hunting, Incident Response, Detection Engineering and Cloud Security, so every course now has at least two lessons (12 courses, 26 lessons).
- New lessons cover log sources/correlation/alerting, threat-intel hypothesis-driven hunting, incident containment/recovery/lessons-learned, detection testing and ATT&CK coverage mapping, and cloud logging/posture management - the day-to-day SOC skills SC-200 and CySA+ test hardest.
- All lessons keep the six education-standard sections and the ~18/82 theory/lab split; content-standard and coverage tests still pass (197 backend tests).

## Phase 2 - Timed practice exams

### Certification practice exams
- New exam engine samples quiz questions across the lessons mapped to a certification's objectives, spread round-robin so breadth of readiness is tested rather than one lesson.
- API under `/api/certifications/{cert_id}/exam`: exam info, a reshuffled paper of 5-100 questions, scored submission, and attempt history (`12_API.md`).
- Graded at an 80% pass mark with a per-objective breakdown; each sitting is recorded in the new `exam_attempts` table with its duration and breakdown.
- Desktop "Practice Exams" page: pick a certification and length, a timed sitting, then a score with highlighted focus areas linking to courses.
- 15 backend tests and 13 desktop view-model tests cover sampling, grading, the breakdown and the exam flow.

## Phase 2 - Learning content

### Full certification coverage
- Added 4 courses and 9 lessons (Security Foundations, Vulnerability Management, Security Architecture, Governance/Risk/Compliance): 12 courses and 21 lessons total.
- Every objective of Security+, SC-900, SC-200 and CySA+ now maps to at least one lesson; catalog is ~18% theory / 82% lab.
- New tests enforce full Security+ objective coverage and full coverage of every priority certification.

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
